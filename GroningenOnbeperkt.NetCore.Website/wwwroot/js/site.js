//$ = require('jquery');

// Write your Javascript code.
// TileLayer maps
var osmMap = L.tileLayer('http://{s}.tile.osm.org/{z}/{x}/{y}.png', {
    id: '1',
    maxZoom: 20,
    maxNativeZoom: 18,
    attribution: '&copy; <a href="http://osm.org/copyright">OpenStreetMap</a> contributors'
}),
    wikiMap = L.tileLayer('https://maps.wikimedia.org/osm-intl/{z}/{x}/{y}.png', {
        id: '2',
        maxZoom: 20,
        maxNativeZoom: 18,
        attribution: '&copy; <a href="http://osm.org/copyright">OpenStreetMap</a> contributors'
    }),
    mapboxMap = L.tileLayer('https://api.mapbox.com/styles/v1/mapbox/{id}/tiles/{z}/{x}/{y}?access_token={accessToken}', {
        attribution: 'Map data &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors, <a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, Imagery © <a href="http://mapbox.com">Mapbox</a>',
        maxZoom: 20,
        title: true,
        id: 'streets-v9',
        accessToken: 'pk.eyJ1IjoiZWFsemVuIiwiYSI6ImNqMHc1Z2QzZTAwMGMzMm1teGFoajZyd3MifQ.92WzXSKZDTETvkG7iFCQ2g'
    });

var baseLayers = {
    "MapBoxMap": mapboxMap,
    "OpenStreetMap": osmMap,
    "WikiMediaMap": wikiMap
};

var testForWheelchairOverLayClick = new WheelchairWayOverPassLayer();

var overlays = {
    "WheelchairAccess": testForWheelchairOverLayClick
};

// init Leaflet map
var map = L.map('map', { preferCanvas: true, zoomControl: false, layers: [wikiMap] }).setView([53.217027, 6.566808], 14);

// init Sidebar
var sidebar = L.control.sidebar('sidebar').addTo(map);

L.control.zoom({
    position: 'bottomright'
}).addTo(map);

// routing
var control = L.Routing.control(L.extend({
    router: L.Routing.osrmv1({
        serviceUrl: osrmServiceUrl,
        language: 'nl'
    }),
    geocoder: L.Control.Geocoder.mapzen('mapzen-F9G1MKe', {
        'boundary.country': 'NL'
    }),
    language: 'nl',
    routeWhileDragging: true,
    reverseWaypoints: true,
    showAlternatives: true,
    altLineOptions: {
        styles: [
            { color: 'black', opacity: 0.15, weight: 9 },
            { color: 'white', opacity: 0.8, weight: 6 },
            { color: 'blue', opacity: 0.5, weight: 2 }
        ]
    }
}));

// navigation tab
control._map = map;
var controlDiv = control.onAdd(map);
document.getElementById('navigation').appendChild(controlDiv);

// search tab
var geocoderSearch = new L.Control.geocoder({ geocoder: L.Control.Geocoder.mapzen('search-DopSHJw', { 'boundary.country': 'NL' }) }).onAdd(map); //{ geocoder: L.Control.Geocoder.mapzen('search-DopSHJw', { 'boundary.country': 'NL' }) }
document.getElementById('searchField').appendChild(geocoderSearch);


// layers tab
function showBaseLayer(destLayer) {
    for (var base in baseLayers) {
        if (map.hasLayer(baseLayers[base]) && baseLayers[base] !== destLayer) {
            map.removeLayer(baseLayers[base]);
        }
    }
    map.addLayer(baseLayers[destLayer]);
}

// poi tab - filters
var poiMcg = L.markerClusterGroup({
    chunkedLoading: true,
    chunkInterval: 400,
    spiderfyOnMaxZoom: false,
    animate: false,
    maxClusterRadius: 120,
    showCoverageOnHover: false,
    animateAddingMarkers: false,
    disableClusteringAtZoom: 17,
    removeOutsideVisibleBounds: true
}).addTo(map);

var setClustering = function (state) {
    if (state.checked)
        poiMcg.enableClustering();
    else
        poiMcg.disableClustering();
};

var hiddenAllLayers = { 'yesLayer': true, 'limitedLayer': true, 'noLayer': true, 'unknownLayer': true };
var poiFilters = {};
function hideShowWheelchairAccessLayer(layerName, filter, self) { //self not used
    if (layerName in poiFilters) {
        poiFilters[layerName].hideShowAllLayers();
    } else {
        poiFilters[layerName] = new PoiOverPassLayer(filter, poiMcg, $.extend(true, {}, hiddenAllLayers));
        map.addLayer(poiFilters[layerName]);
    }
}

function hideShowLayer(layerName, destLayer) {
    if (poiFilters[layerName] != null) {
        poiFilters[layerName].hideShowLayer(destLayer, $('#filter-' + layerName + '-main-button').is(':checked'));
    }
}

function setVisibilityAllLayers(destLayer, button) {
    $('[id ^=' + destLayer + '][id $=filter]').each(function (index) {
        if (this.classList.contains('notActiveFilterLayer') === button.classList.contains('notActiveFilterLayer')) {
            this.click();
        }
    });
    hiddenAllLayers[destLayer] = button.classList.contains('notActiveFilterLayer');
}

// edit tab
function showWheelchairAccessLayer(destLayer, id) {
    var actor = $('#' + id);
    var checked = actor.prop('checked');
    var group = actor.data('group');
    var checkboxes = $('input[type="checkbox"][data-group="' + group + '"]');
    var otherCheckboxes = checkboxes.not(actor);
    otherCheckboxes.prop('checked', checked);

    if (map.hasLayer(overlays[destLayer])) {
        overlays[destLayer].hideLayer();
    } else { //first time
        map.addLayer(overlays[destLayer]);
    }
}

// settings tab

//set location (Rightclick)
map.on('contextmenu', function (e) {
    if (control.getWaypoints()[0].latLng) {
        control.spliceWaypoints(control.getWaypoints().length - 1, 1, e.latlng);
    } else {
        control.spliceWaypoints(0, 1, e.latlng);
    }
});


//getWayKeys
function getKeysByWayId(id, editedWays) {
    var url = 'http://www.openstreetmap.org/api/0.6/way/' + id;
    $.ajax({
        url: url,
        type: "GET",
        dataType: "xml",
        success: function (data) {
            var result = [];
            $(data).find('tag').each(function () {
                var temp = [];
                $.each(this.attributes, function (i, attrib) {
                    temp.push(this.value);
                });
                result[temp[0]] = temp[1];
            });
            var editedKeysWay = $.grep(editedWays, function (e) { if (e.id === id) e.keys = result; });
        }
    });
}