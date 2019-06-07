var loadingPoiCount = 0;

function PoiOverPassLayer(poi, poiCluster, layerVisibility) {
    var endpoint = "http://overpass.osm.rambler.ru/cgi/";// Alternative is "http://overpass-api.de/api/"
    var wheelchairPoiColors = { yes: '#33CC33', limited: '#FFFF00', no: '#FF8080', unknown: '#b7b7b7' };
    var layersAreVisible = false;

    var options = {
        endpoint: endpoint,
        minZoom: 13,
        layerVisibleCount: 4, //lengte van childlayers
        query: 'node({{bbox}})' + poi + ';out qt;',
        childLayers: { 'yesLayer': null, 'limitedLayer': null, 'noLayer': null, 'unknownLayer': null }, // bevat layers
        childLayersVisible: layerVisibility, // voorkomt dat nieuwe poi's niet zichtbaar worden
        markerCluster: poiCluster,

        beforeRequest: function () {
            if (loadingPoiCount === 0) {
                $('#loadPoiInfo').text('Poi\'s het laden...');
            }
            loadingPoiCount++;
        },

        afterRequest: function () {
            if (loadingPoiCount === 1 )
                $('#loadPoiInfo').text('');
            loadingPoiCount--;
        },

        onSuccess: function (data) {

            for (var i = 0; i < data.elements.length; i++) {
                var l = data.elements[i];
                if (l) {

                    if (l.id in this._ids) {
                        continue;
                    }

                    var type = getWheelchairType(l.tags);
                    this._ids[l.id] = type;
                    var iconName = l.tags.amenity;

                    var marker = L.marker(L.latLng(l.lat, l.lon), {
                        icon: L.divIcon({
                            className: 'leaflet-marker-poi-icon marker-wheelchair-' + type,
                            html: '<img src="data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA=" class="icons ' + iconName + '"></img>'
                        })
                    });
                    var informationHtml = getHtmlInfo(l.tags, l.id);

                    (function (infoHtml) {
                        marker.on('click', function (e) {
                            $('#informationContent').html(infoHtml);
                            if (!$("#information").hasClass("active")) {

                                $('#informationContentTab')[0].click();

                            }

                        });
                    })(informationHtml);

                    switch (type) {
                        case 'yes':
                            this.options.childLayers['yesLayer'].addLayer(marker);
                            break;
                        case 'no':
                            this.options.childLayers['noLayer'].addLayer(marker);
                            break;
                        case 'limited':
                            this.options.childLayers['limitedLayer'].addLayer(marker);
                            break;
                        default:
                            this.options.childLayers['unknownLayer'].addLayer(marker)
                    }
                }
            }
            for (var name in this.options.childLayers) {
                if (this.options.childLayersVisible[name] && !layersAreVisible) {
                    this._map.addLayer(this.options.childLayers[name]);
                } else {
                    this._map.removeLayer(this.options.childLayers[name]);
                }
            }
        }

    }

    var getHtmlInfo = function (tags, id) {
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

    var hideShowAllLayers = function () {
        var isVisible = false;
        for (var key in this.options.childLayersVisible) {
            if (this.options.childLayersVisible[key] === true) {
                isVisible = true;
                break;
            }
        }

        if (!isVisible && !layersAreVisible)
            return;

        for (var value in this.options.childLayersVisible) {
            if (this.options.childLayersVisible[value] === isVisible) {

                var oldVisibleValue = this.options.childLayersVisible[value];
                this.hideShowLayer(value, true);
                this.options.childLayersVisible[value] = oldVisibleValue;
            }
        }

        this.enableZoomControl(this.options.layerVisibleCount > 0);
        layersAreVisible = !layersAreVisible;
    }

    var hideShowLayer = function (layerName, isvisbl) {
        if (!isvisbl) {
            this.options.childLayersVisible[layerName] = !this.options.childLayersVisible[layerName];
        }
        else {
            if (this._map.hasLayer(this.options.childLayers[layerName])) {
                this._map.removeLayer(this.options.childLayers[layerName]);
                this.options.childLayersVisible[layerName] = false;
                this.options.layerVisibleCount--;
            } else {
                this._map.addLayer(this.options.childLayers[layerName]);
                this.options.childLayersVisible[layerName] = true;
                this.options.layerVisibleCount++;
            }
            this.enableZoomControl(this.options.layerVisibleCount > 0);
        }
    }

    var getWheelchairType = function (tags) {
        try {
            if (tags.wheelchair === "yes") {
                return 'yes';
            } else if (tags.wheelchair === "limited") {
                return 'limited';
            } else if (tags.wheelchair === "no") {
                return 'no';
            } else return 'unknown';
        }
        catch (err) {
            return 'unknown';
        }
    }

    var layer = new L.OverPassLayer(options);
    layer.hideShowLayer = hideShowLayer;
    layer.hideShowAllLayers = hideShowAllLayers;

    return layer;
}