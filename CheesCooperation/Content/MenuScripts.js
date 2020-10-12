$('#changeNameLink').click(function (event) {
	event.preventDefault();
	$.cookie('user_name', $("#user_name").val(), { 'path': '/' });
});