function loading() {
    $.blockUI({
        css: {
            border: 'none',
            padding: '15px',
            backgroundColor: '#000',
            '-webkit-border-radius': '10px',
            '-moz-border-radius': '10px',
            opacity: .5,
            color: '#fff'
        }
    });
}

$(document).ready(function () {
    $('#entertainment-link').click(function () {
        loading();
    });
    $('#sports-link').click(function () {
        loading();
    });
    $('#mainland-link').click(function () {
        loading();
    });
    $('#finance-link').click(function () {
        loading();
    });
    $('#health-link').click(function () {
        loading();
    });
    $('#politics-link').click(function () {
        loading();
    });
    $('#society-link').click(function () {
        loading();
    });
});