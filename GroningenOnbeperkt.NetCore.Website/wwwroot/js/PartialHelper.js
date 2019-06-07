
function saveEdit() {
    testForWheelchairOverLayClick.saveEdit();
}
               

function switchVisible(id) {
            if (document.getElementById(id)) {
                if (document.getElementById(id).style.display === 'none') {
                    document.getElementById(id).style.display = 'block';
                }
                else {
                    document.getElementById(id).style.display = 'none';
                }
            }
}

function switchButtonActiveLayer(id, className) {
    var element = document.getElementById(id);

    if (document.getElementById(id)) {
        if (element.classList.contains(className)) {
                element.classList.remove(className);
        }
        else {
                element.classList.add(className);
        }
    }
}
