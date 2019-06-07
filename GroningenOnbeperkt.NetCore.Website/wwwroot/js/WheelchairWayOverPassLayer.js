var loadingWayCount = 0;
function WheelchairWayOverPassLayer() {
    var endpoint = "http://overpass-api.de/api/"; // Alternative is "http://overpass.osm.rambler.ru/cgi/";
    var wheelchairWayColors = { yes: '#33CC33', limited: '#FFFF00', no: '#FF8080', unknown: '#b7b7b7' };
    var waySelection = { color: 'blue', opacity: 0.4, weight: 5 };
    var selectedWays = []; //{"id": id, "line: line, "trackStyle": defaultTrackStyle}
    var editedWays = []; //{"id": id, "trackStyle": defaultTrackStyle}

    var setEditedWays = function (ways, wheelchairAccessibility) {
        for (var i = 0; i < ways.length; i++) {
            editedWays.push({ id: ways[i].id, trackStyle: { color: wheelchairWayColors[wheelchairAccessibility], opacity: 0.8, weight: 3 }, keys: [] });
            ways[i].line.setStyle({ color: wheelchairWayColors[wheelchairAccessibility], opacity: 0.8, weight: 3 });
            $('#' + ways[i].id).remove();
            getKeysByWayId(ways[i].id, editedWays);
        }
        selectedWays = [];
    }

    var findEditedWay = function (wayId) {
        for (var i = 0, len = editedWays.length; i < len; i++) {
            if (editedWays[i].id === wayId)
                return editedWays[i];
        }
        return null;
    }

    var options = {
        endpoint: endpoint,
        minZoom: 16,
        groupName: 'wegen',
        query: 'way({{bbox}})["highway"];out geom;',
        beforeRequest: function () {
            if (loadingWayCount === 0) {
                $('#loadWayInfo').text('Wegen aan het laden...');
                $('#loadEditWayInfo').text('Wegen aan het laden...');
            }
            loadingWayCount++;
        },

        afterRequest: function () {
            if (loadingWayCount === 1) {
                $('#loadWayInfo').text('');
                $('#loadEditWayInfo').text('');
            }
            loadingWayCount--;
        },
        onSuccess: function (data) {

            for (var i = 0; i < data.elements.length; i++) {
                var l = data.elements[i];
                if (l && l.tags.highway !== null) {

                    if (l.id in this._ids) {
                        continue;
                    }

                    this._ids[l.id] = true;
                    var latLngs = [];

                    for (var f = 0; f < l.geometry.length; f++) {
                        latLngs[latLngs.length] = new L.latLng(l.geometry[f].lat, l.geometry[f].lon);
                    }

                    var trackStyle = getHighwayStyle(l.tags);
                    if (trackStyle) {
                        var line = L.polyline(latLngs, trackStyle);
                        var informationHtml = getHighwayHtmlInfo(l.tags, l.id);

                        (function (l, trackStyle, informationHtml) {
                            line.on('click', function (e) {

                                var id = l.id;
                                var infoHtml = informationHtml;
                                var line = e.target;
                                var defaultTrackStyle = trackStyle;

                                var result = findEditedWay(id);

                                if (result !== null) {
                                    defaultTrackStyle = result.trackStyle;
                                    infoHtml = getHighwayHtmlInfo(result.keys, id);
                                }

                                if (!e.originalEvent.shiftKey) {
                                    selectedWays = $.grep(selectedWays, function (z) {
                                        if (z.id !== id) {
                                            z.line.setStyle(z.trackStyle);
                                            $('#' + z.id).remove();
                                            return;
                                        }
                                        return z.id;
                                    });
                                }

                                if (selectedWays.filter(function (e) { return e.id === id; }).length > 0) {
                                    line.setStyle(defaultTrackStyle);
                                    $('#' + l.id).remove();
                                    selectedWays = $.grep(selectedWays, function (e) { return e.id !== id; });
                                } else {
                                    line.setStyle(waySelection);
                                    selectedWays.push({ "id": id, "line": line, "trackStyle": defaultTrackStyle });
                                    $('#informationContent').append(infoHtml);
                                }
                            });
                        })(l, trackStyle, informationHtml);

                        this.options.childLayers[Object.keys(this.options.childLayers)[0]].addLayer(line);
                    }
                }
            }
        }

    }

    var getHighwayStyle = function (tags) {
        if (tags !== 'undefined')
            switch (tags.wheelchair) {
                case "yes":
                    return { color: wheelchairWayColors['yes'], opacity: 0.8, weight: 3 };
                case "limited":
                    return { color: wheelchairWayColors['limited'], opacity: 0.8, weight: 3 };
                case "no":
                    return { color: wheelchairWayColors['no'], opacity: 0.8, weight: 3 };
                default:// 
                    if (tags.highway === "steps") {
                        return { color: wheelchairWayColors['no'], opacity: 0.8, weight: 3 };
                    } else { //unknown 
                        return { color: wheelchairWayColors['unknown'], opacity: 0.0, weight: 3 };
                    }
            }
    }

    var getHighwayHtmlInfo = function (tags, id) {
        var row,
            table = document.createElement('table'),
            div = document.createElement('div');

        table.style.borderSpacing = '10px';
        table.style.borderCollapse = 'separate';

        for (var key in tags) {

            row = table.insertRow(0);
            row.insertCell(0).appendChild(document.createTextNode(key));
            row.insertCell(1).appendChild(document.createTextNode(tags[key]));
        }

        div.id = id;
        div.appendChild(table);

        return div;
    }

    var hideLayer = function () {
        if (this._map.hasLayer(this.options.childLayers[Object.keys(this.options.childLayers)[0]])) {
            this._map.removeLayer(this.options.childLayers[Object.keys(this.options.childLayers)[0]]);
            this._zoomControl._removeLayer(this);
        } else {
            this._map.addLayer(this.options.childLayers[Object.keys(this.options.childLayers)[0]]);
            this._zoomControl._addLayer(this);
        }
    }

    var saveEdit = function () {
        var ways = selectedWays;
        if (ways !== 'undefined' && ways.length !== 0) {
            var language = $("#Language").val();

            var wheelchairAccessibility = $('input[name=radio]:checked').val();
            if (wheelchairAccessibility === 'undefined') {
                wheelchairAccessibility = '';
            }
            var description = $("#description").val();

            $.ajax({
                url: "/Home/EditOpenstreetmapAccessibility",
                type: 'POST',
                data: { "ways": $.map(ways, function (data) { return data.id; }), "wheelchairAccessibility": wheelchairAccessibility, "description": description, "language": language },
                success: function (message) {
                    $("#infoLabel").text(message);
                    setEditedWays(ways, wheelchairAccessibility);
                }
            });
        } else {
            $("#infoLabel").text("Er is geen weg geselecteerd.");
        }
    }

    var layer = new L.OverPassLayer(options);
    layer.hideLayer = hideLayer;
    layer.saveEdit = saveEdit;
    return layer;
}