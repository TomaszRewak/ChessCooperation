var squares;
var pieces;
var piecesThatCanMove;
var actualPiecesStates
// gameName
// isPlayer

function generateTable() {
	var board = $('#boardCanvas');
	var table = $('<table></table>');
	board.append(table);
	table.addClass('boardTable');

	squares = new Array(8);
	for (i = 0; i < 8; i++)
		squares[i] = new Array(8);

	for (i = 0; i < 10; i++) {
		var row = $('<tr></tr>');
		table.append(row);

		for (j = 0; j < 10; j++) {
			var square = $('<td></td>');
			row.append(square);

			square.addClass('square');

			if (i == 0 || j == 0 || i == 9 || j == 9) {
				square.addClass('sideSquare');

				if (j > 0 && j < 9)
					square.append(String.fromCharCode(64 + j));
				if (i > 0 && i < 9)
					square.append((9 - i).toString());

				square.droppable({
					accept: ".piece",
					drop: function (event, ui) {
						$.connection.gameHub.server.moveTo(gameName, -1, -1, -1, -1);
					}
				});
			}
			else {
				square.addClass('pieceSquare');

				var indicator = $('<div></div>');
				square.append(indicator);
				indicator.addClass('squareIndicator');

				if ((i + j) % 2 == 0)
					square.addClass('whiteSquare');
				else
					square.addClass('blackSquare');

				squares[8 - i][j - 1] = square;

				square.data('Y', 8 - i);
				square.data('X', j - 1);

				square.droppable({
					accept: ".piece",
					drop: function (event, ui) {
						var hub = $.connection.gameHub;

						var pieceID = ui.helper.data('id');
						var x1 = actualPiecesStates[pieceID].X;
						var y1 = actualPiecesStates[pieceID].Y;

						var x2 = $(this).data('X');
						var y2 = $(this).data('Y');

						hub.server.moveTo(gameName, x1, y1, x2, y2);
					}
				});
			}
		}
	}

	$('#boardPlaceholderText').hide();
}

function resizeBoard() {
	var board = $('#boardCanvas');
	var boardHolder = $('#gameHolder');
	if (board != null && boardHolder != null) {
		var width = boardHolder.width();
		var height = boardHolder.height();

		var a = Math.min(width, height) * 0.95;
		a -= a % 10;

		board.css({ 'height': a + 'px' });
		board.css({ 'width': a + 'px' });
		board.css({ 'margin-top': (height - a) / 2 + 'px' })

		for (i = 0; i < 32; i++) {
			if (pieces[i] != null) {
				pieces[i].css({ 'font-size': (a / 13) + 'px' })
				pieces[i].css({ 'line-height': (a / 10) + 'px' })
			}
		}
	}
}

function generateListOfPiecesToMove() {
	piecesThatCanMove = new Array(0);
	for (i = 0; i < 32; i++) {
		var piece = actualPiecesStates[i];
		if (piece.CanMove) {
			piecesThatCanMove[piecesThatCanMove.length] = {
				Y: piece.Y, X: piece.X
			};
		}
	}
}

function highLightSquares(listOfFields, basicColor) {
	var color;
	if (typeof (basicColor) === 'undefined' || basicColor == 'true')
		color = '#199b1c';
	else
		color = '#1355f5';

	if (typeof (basicColor) !== 'undefined')
		for (i = 0; i < 8; i++) {
			for (j = 0; j < 8; j++) {
				squares[i][j].find('.squareIndicator').stop(true, false).animate({
					opacity: '0.0'
				}, 'fast');
			}
		}

	for (i = 0; i < listOfFields.length; i++) {
		squares[listOfFields[i].Y][listOfFields[i].X].find('.squareIndicator').css({ backgroundColor: color });

		squares[listOfFields[i].Y][listOfFields[i].X].find('.squareIndicator').stop(true, false).animate({
			opacity: '0.4'
		}, 'fast');
	}
}

var lastUpdate = new Date();
function addPieces() {
	pieces = new Array(32);

	var canvas = $('#boardCanvas');

	for (i = 0; i < 32; i++) {

		var piece = $('<div></div>');
		canvas.append(piece);
		piece.addClass('piece');
		piece.data('id', i);
		piece.css('left', 45 + '%');
		piece.css('top', 45 + '%');

		piece.draggable({
			containment: 'parent',

			start: function (event, ui) {
				var hub = $.connection.gameHub;

				ui.helper.stop(true, false);
				var pieceID = ui.helper.data('id');
				var x = actualPiecesStates[pieceID].X;
				var y = actualPiecesStates[pieceID].Y;

				hub.server.startMove(gameName, x, y);
			},

			drag: function (event, ui) {
				ui.helper.stop(true, false);

				var time = new Date();
				if (time - lastUpdate > 100) {
					lastUpdate = time;

					var hub = $.connection.gameHub;

					var pieceID = ui.helper.data('id');
					var l = (100.0 * ui.position.left / parseFloat(ui.helper.parent().width()));
					var t = (100.0 * ui.position.top / parseFloat(ui.helper.parent().height()));

					hub.server.dragTo(gameName, pieceID, l, t);
				}
			},

			stop: function (event, ui) {
				var l = (100 * ui.position.left / parseFloat(ui.helper.parent().width())) + '%';
				var t = (100 * ui.position.top / parseFloat(ui.helper.parent().height())) + '%';
				$(this).css('left', l);
				$(this).css('top', t);
			}
		});

		pieces[i] = piece;
	}
}

function updateGameState(newPiecesState, tourTime, showMoves) {
	time = Date.now() + tourTime * 1000;

	actualPiecesStates = newPiecesState;
	if (typeof actualPiecesStates != null) {
		for (i = 0; i < 32; i++) {
			var pieceInfo = actualPiecesStates[i];
			var piece = pieces[i];

			if (pieceInfo.OnBoard) {
				piece.html(String.fromCharCode(pieceInfo.Symbol));

				//if (pieceInfo.Symbol == 9812)
				//	piece.html('<img height=100% src="http://bi.gazeta.pl/im/5/7995/z7995585Q,Janusz-Korwin-Mikke.jpg">');
				//if (pieceInfo.Symbol == 9818)
				//	piece.html('<img height=100% src="http://infolinia.org/poradnik/files/wydarzenia/75/Janusz_Korwin_Mikke.jpg">');

				piece.fadeIn();
				piece.animate({
					left: 10 * (1 + pieceInfo.X) + '%',
					top: 10 * (8 - pieceInfo.Y) + '%',
					width: '10%',
					height: '10%'
				});
			}
			else {
				piece.fadeOut();
			}
		}
	}

	generateListOfPiecesToMove();
	if (showMoves)
		highLightSquares(piecesThatCanMove, false);
}

function dragTo(ID, x, y) {
	if (pieces[ID] != null) {
		pieces[ID].stop(true, false).animate({
			left: x + '%',
			top: y + '%'
		}, 200);
	}
}

function addToChat(name, time, text) {
	var message = $('<div><\div>');
	message.addClass('chatMessage');

	var messageName = $('<div><\div>');
	messageName.addClass('chatMessageName');
	messageName.append(name);

	var messageDate = $('<div><\div>');
	messageDate.addClass('chatMessageTime');
	messageDate.append(time);

	var messageText = $('<div><\div>');
	messageText.addClass('chatMessageText');
	messageText.append(text);

	var hr = $('<hr>');

	message.append(messageName);
	message.append(messageDate);
	message.append(hr);
	message.append(messageText);

	$('#chat').append(message);

	$('#chat').stop(true, false).animate({ scrollTop: $('#chat').prop("scrollHeight") }, 500);
}

function addChatAction() {
	$('#chatForm').submit(function (event) {
		event.preventDefault();
		var hub = $.connection.gameHub;
		hub.server.sendMessage(gameName, $('#chatInput').val());
		$('#chatInput').val('');
		return false;
	});
}

var time = Date.now();
function timer() {
	var t = Math.floor((time - Date.now()) / 1000);
	if (t>=0)
		$('.timer').html(t.toString() + 's');
	else
		$('.timer').html('---');
}

var hidden = true;
function hideChatButtonClicked() {
	if (hidden) {
		$('#gameHolder').removeClass('col-xs-12');
		$('#gameHolder').addClass('col-sm-8 hidden-xs');

		$('#chatHolder').addClass('col-sm-4 col-xs-12');

		$('#gameInfoHidden').hide();
	}
	else {
		$('#gameHolder').removeClass('col-sm-8 hidden-xs');
		$('#gameHolder').addClass('col-xs-12');

		$('#chatHolder').removeClass('col-sm-4 col-xs-12');

		$('#gameInfoHidden').show();
	}

	hidden = !hidden;
	resizeBoard();
}

function goToMenuButtonClick() {
	location.href = "/"
}

$(function () {
	// Reference the auto-generated proxy for the hub.  
	var hub = $.connection.gameHub;
	// Create a function that the hub can call back to display messages.
	hub.client.highLightFields = highLightSquares;
	hub.client.updateGameState = updateGameState;
	hub.client.dragTo = dragTo;
	hub.client.addToChat = addToChat;

	// Start the connection.
	$.connection.hub.start().done(function () {
		generateTable();
		$(window).resize(resizeBoard);
		addPieces();
		// resizeBoard(); called in hideChatButtonClicked
		hideChatButtonClicked();
		hub.server.joinRoom(gameName, isPlayer);
		addChatAction();
		setInterval(timer, 1000);
	});
});
