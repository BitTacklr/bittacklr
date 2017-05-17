function initMaps() {
    var styles = [
        {
            "elementType": "geometry",
            "stylers": [
                {
                    "color": "#f5f5f5"
                }
            ]
        },
        {
            "elementType": "labels.icon",
            "stylers": [
                {
                    "visibility": "off"
                }
            ]
        },
        {
            "elementType": "labels.text.fill",
            "stylers": [
                {
                    "color": "#616161"
                }
            ]
        },
        {
            "elementType": "labels.text.stroke",
            "stylers": [
                {
                    "color": "#f5f5f5"
                }
            ]
        },
        {
            "featureType": "administrative.land_parcel",
            "elementType": "labels.text.fill",
            "stylers": [
                {
                    "color": "#bdbdbd"
                }
            ]
        },
        {
            "featureType": "poi",
            "elementType": "geometry",
            "stylers": [
                {
                    "color": "#eeeeee"
                }
            ]
        },
        {
            "featureType": "poi",
            "elementType": "labels.text.fill",
            "stylers": [
                {
                    "color": "#757575"
                }
            ]
        },
        {
            "featureType": "poi.park",
            "elementType": "geometry",
            "stylers": [
                {
                    "color": "#e5e5e5"
                }
            ]
        },
        {
            "featureType": "poi.park",
            "elementType": "labels.text.fill",
            "stylers": [
                {
                    "color": "#9e9e9e"
                }
            ]
        },
        {
            "featureType": "road",
            "elementType": "geometry",
            "stylers": [
                {
                    "color": "#ffffff"
                }
            ]
        },
        {
            "featureType": "road.arterial",
            "elementType": "labels.text.fill",
            "stylers": [
                {
                    "color": "#757575"
                }
            ]
        },
        {
            "featureType": "road.highway",
            "elementType": "geometry",
            "stylers": [
                {
                    "color": "#dadada"
                }
            ]
        },
        {
            "featureType": "road.highway",
            "elementType": "labels.text.fill",
            "stylers": [
                {
                    "color": "#616161"
                }
            ]
        },
        {
            "featureType": "road.local",
            "elementType": "labels.text.fill",
            "stylers": [
                {
                    "color": "#9e9e9e"
                }
            ]
        },
        {
            "featureType": "transit.line",
            "elementType": "geometry",
            "stylers": [
                {
                    "color": "#e5e5e5"
                }
            ]
        },
        {
            "featureType": "transit.station",
            "elementType": "geometry",
            "stylers": [
                {
                    "color": "#eeeeee"
                }
            ]
        },
        {
            "featureType": "water",
            "elementType": "geometry",
            "stylers": [
                {
                    "color": "#c9c9c9"
                }
            ]
        },
        {
            "featureType": "water",
            "elementType": "labels.text.fill",
            "stylers": [
                {
                    "color": "#9e9e9e"
                }
            ]
        }
    ];
    var coordinates = { lat: 51.2378881, lng: 2.9713747 };
    var smallMap = new google.maps.Map(document.getElementById('smallmap'), {
        zoom: 5,
        center: coordinates,
        mapTypeId: 'roadmap',
        styles: styles
    });
    var smallMarker = new google.maps.Marker({
        position: coordinates,
        map: smallMap,
        icon: 'images/mapmarker.svg'
    });
    var largeMap = new google.maps.Map(document.getElementById('largemap'), {
        zoom: 5,
        center: coordinates,
        mapTypeId: 'roadmap',
        styles: styles
    });
    var largeMarker = new google.maps.Marker({
        position: coordinates,
        map: largeMap,
        icon: 'images/mapmarker.svg'
    });

    // resize map upon window resize
    google.maps.event.addDomListener(window, "resize", function() {
        var smallMapElement = document.getElementById('smallmap');
        if (smallMapElement != undefined && smallMapElement.style.display !== "none") {
            var smallCenter = smallMap.getCenter();
            google.maps.event.trigger(smallMap, "resize");
            smallMap.setCenter(smallCenter);
        }
        
        var largeMapElement = document.getElementById('largemap');
        if (largeMapElement != undefined && largeMapElement.style.display !== "none") {
            var largeCenter = largeMap.getCenter();
            google.maps.event.trigger(largeMap, "resize");
            largeMap.setCenter(largeCenter);
        }
    });
}
