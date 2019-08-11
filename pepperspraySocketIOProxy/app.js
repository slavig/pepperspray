'use strict';

var ADDRESS = "localhost";
var PORT = 8081;

const { createLogger, format, transports } = require("winston");
const net = require('net');
const http = require('http');
const socketIo = require('socket.io');

const logger = createLogger({
	level: "info",
	format: format.combine(
		format.timestamp({
			format: "YYYY-MM-DD HH:mm:ss"
		}),
		format.errors({ stack: true }),
		format.splat(),
		format.json()
	),

	transports: [
		new transports.File({ filename: "app.log" })
	]
});

if (process.env.NODE_ENV !== 'production') {
	logger.add(new transports.Console({
		format: format.combine(
			format.colorize(),
			format.simple()
		)
	}));
}

var httpServer = http.Server();
var socketServer = socketIo(httpServer);
socketServer.on("connection", socket => {
	var address = socket.handshake.address;

	logger.info("socket.io connected", { at: address });
	var client = new net.Socket();
	client.connect(PORT, ADDRESS, () => {
		logger.info("client connected", { for: address });
		client.write(JSON.stringify(["addr", address]));
	});

	client.on("close", function () {
		logger.info("client closed");
		socket.disconnect();
	});

	socket.on("disconnect", () => {
		logger.info("socket.io disconnected");
		client.destroy();
	});

	client.on("error", e => {
		logger.warn("client failed: ", { error: e });
		socket.disconnect();
	});

	socket.on("error", e => {
		logger.warn("socket.io failed: ", { error: e });
		client.destroy();
	});

	var proxy = (args) => {
		logger.info("socket.io received, sending to client", { args: args });
		client.write(JSON.stringify(Array.from(args)));
	};

	socket.on("login request", (username, password, version) => {
		proxy(["login request", username, password, version]);
	});

	socket.on("retokennect", (token) => {
		proxy(["retokennect", token]);
	});

	client.on("data", data => {
		logger.info("client received, sending to socket", { bytes: data.length });
		socket.emit.apply(socket, JSON.parse(data));
	});
});

logger.info("pepperspraySocketIOProxy v0.1");
httpServer.listen(3002, () => logger.info("listening on ", { address: ADDRESS, port: 3002 }));
