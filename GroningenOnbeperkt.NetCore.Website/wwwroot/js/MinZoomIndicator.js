var MinZoomIndicator = L.Control.extend({

    options: {

    },

    _layers: {},
    _names: {},

    initialize: function (options) {

        L.Util.setOptions(this, options);

        this._layers = {};
        this._names = {};
    },

    _addLayer: function (layer) {

        var minZoom = 15;
        var groupName;

        if (layer.options.minZoom) {

            minZoom = layer.options.minZoom;
            groupName = layer.options.groupName;
        }

        this._layers[layer._leaflet_id] = minZoom;
        this._names[layer._leaflet_id] = groupName;

        this._updateBox(null);
    },

    _removeLayer: function (layer) {

        this._layers[layer._leaflet_id] = null;
        this._names[layer._leaflet_id] = null;

        this._updateBox(null);
    },

    _hasLayer: function (layer) {
        var test = this._layers[layer._leaflet_id];
        return !(test === 'undefined' || test === null);
    },


    _getMinZoomLevel: function () {

        var key,
            minZoomLevel = - 1;

        for (key in this._layers) {

            if (this._layers[key] !== null && this._layers[key] > minZoomLevel) {

                minZoomLevel = this._layers[key];
            }
        }

        return minZoomLevel;
    },

    _getDataNamesForMessage: function () {
        var message = '';
        var names = [];

        var currentZoom = this._map.getZoom();
        for (key in this._layers) {

            if (this._layers[key] !== null && this._layers[key] > currentZoom && this._layers !== '') {
                if ($.inArray(this._names[key], names) == -1)  names.push(this._names[key]);
            }
        }

        switch (names.length) {
            case 0:
                return '';
            case 1:
                return names[0];
            default:
                message = names.join(", ");
                if (message.match(/,/g).length > 0)
                    message = message.replace(',', ' en');
                return message;
        }
    },


    _updateBox: function (event) {

        var minZoomLevel = this._getMinZoomLevel();

        if (event !== null) {

            L.DomEvent.preventDefault(event);
        }

        if (minZoomLevel === -1) {

            this._container.innerHTML = this.options.minZoomMessageNoLayer;
        } else {
            if (this.options.minZoomMessage.search(/DATANAMES/) !== -1) {
                this._container.innerHTML = this.options.minZoomMessage.replace(/DATANAMES/, this._getDataNamesForMessage());
            } else {
                this._container.innerHTML = this.options.minZoomMessage
                    .replace(/CURRENTZOOM/, this._map.getZoom())
                    .replace(/MINZOOMLEVEL/, minZoomLevel);
            }

        }

        if (this._map.getZoom() >= minZoomLevel) {

            this._container.style.display = 'none';
        } else {

            this._container.style.display = 'block';
        }
    },

    onAdd: function (map) {

        this._map = map;

        this._map.zoomIndicator = this;

        this._container = L.DomUtil.create('div', 'leaflet-control-minZoomIndicator');

        this._map.on('moveend', this._updateBox, this);

        this._updateBox(null);

        return this._container;
    },

    onRemove: function (map) {

        // L.Control.prototype.onRemove.call(this, map);

        map.off({

            'moveend': this._updateBox
        }, this);

        this._map = null;
    },
});


L.Control.MinZoomIndicator = MinZoomIndicator;

