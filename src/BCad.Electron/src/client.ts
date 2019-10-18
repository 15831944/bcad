import * as cp from 'child_process';
import * as rpc from 'vscode-jsonrpc';
import { remote } from 'electron';

interface Point3 {
    X: number;
    Y: number;
    Z: number;
}

interface Color {
    A: number;
    R: number;
    G: number;
    B: number;
}

interface Line {
    P1: Point3;
    P2: Point3;
    Color: Color;
}

interface Drawing {
    Lines: Line[];
}

interface ClientUpdate {
    Transform?: number[];
    Drawing?: Drawing;
}

enum MouseButton {
    Left,
    Middle,
    Right,
}

export class Client {
    private connection: rpc.MessageConnection;
    private outputPane: HTMLDivElement;
    private canvas: HTMLCanvasElement;
    private gl: WebGLRenderingContext;
    private drawing: Drawing;
    private transform: number[];
    private coordinatesLocation: number;
    private colorLocation: number;
    private transformLocation: WebGLUniformLocation;
    private bufferSize: number;
    private vertexBuffer: WebGLBuffer;
    private colorBuffer: WebGLBuffer;

    // client notifications
    private ClientUpdateNotification: rpc.NotificationType<ClientUpdate[], void>;
    private MouseDownNotification: rpc.NotificationType<{button: MouseButton, cursorX: Number, cursorY: Number}, void>;
    private MouseUpNotification: rpc.NotificationType<{button: MouseButton, cursorX: Number, cursorY: Number}, void>;
    private MouseMoveNotification: rpc.NotificationType<{cursorX: Number, cursorY: Number}, void>;
    private PanNotification: rpc.NotificationType<{width: Number, height: Number, dx: Number, dy: Number}, void>;
    private ReadyNotification: rpc.NotificationType<{width: Number, height: Number}, void>;
    private ZoomNotification: rpc.NotificationType<{cursorX: Number, cursorY: Number, width: Number, height: Number, delta: Number}, void>;
    private ExecuteCommandRequest: rpc.RequestType1<{command: String}, boolean, void, void>;

    constructor() {
        this.canvas = <HTMLCanvasElement> document.getElementById('canvas');
        this.outputPane = <HTMLDivElement> document.getElementById('output-pane');
        this.ClientUpdateNotification = new rpc.NotificationType<ClientUpdate[], void>('ClientUpdate');
        this.MouseDownNotification = new rpc.NotificationType<{button: MouseButton, cursorX: Number, cursorY: Number}, void>('MouseDown');
        this.MouseUpNotification = new rpc.NotificationType<{button: MouseButton, cursorX: Number, cursorY: Number}, void>('MouseUp');
        this.MouseMoveNotification = new rpc.NotificationType<{cursorX: Number, cursorY: Number}, void>('MouseMove');
        this.PanNotification = new rpc.NotificationType<{width: Number, height: Number, dx: Number, dy: Number}, void>('Pan');
        this.ReadyNotification = new rpc.NotificationType<{width: Number, height: Number}, void>('Ready');
        this.ZoomNotification = new rpc.NotificationType<{cursorX: Number, cursorY: Number, width: Number, height: Number, delta: Number}, void>('Zoom');
        this.ExecuteCommandRequest = new rpc.RequestType1<{command: String}, boolean, void, void>('ExecuteCommand');
    }

    start() {
        this.drawing = {
            Lines: []
        };
        this.transform = [
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        ];
        this.prepareWebGl();
        this.populateVertices();
        this.redraw();
        this.prepareConnection();
        this.prepareEvents();
        this.prepareListeners();
        this.connection.listen();
        this.connection.sendNotification(this.ReadyNotification, {width: this.canvas.width, height: this.canvas.height});
    }

    private prepareConnection() {
        let serverAssembly = "IxMilia.BCad.Server.dll";
        var serverPath = __dirname + '/../bin/' + serverAssembly;
        serverPath = __dirname + '/../../../artifacts/bin/IxMilia.BCad.Server/Debug/netcoreapp3.0/' + serverAssembly;
        let childProcess = cp.spawn('dotnet.exe', [serverPath]);
        childProcess.on('exit', (code: number, _signal: string) => {
            alert('process exited with ' + code);
        });
        console.log('server pid ' + childProcess.pid);
        let logger: rpc.Logger = {
            error: console.log,
            warn: console.log,
            info: console.log,
            log: console.log,
        };
        this.connection = rpc.createMessageConnection(
            new rpc.StreamMessageReader(childProcess.stdout),
            new rpc.StreamMessageWriter(childProcess.stdin),
            logger);
    }

    private zoom(cursorX: Number, cursorY: Number, delta: Number) {
        let width = this.canvas.clientWidth;
        let height = this.canvas.clientHeight;
        this.connection.sendNotification(this.ZoomNotification, {cursorX: cursorX, cursorY: cursorY, width: width, height: height, delta: delta});
    }

    private zoomIn(cursorX: Number, cursorY: Number) {
        this.zoom(cursorX, cursorY, 1);
    }

    private zoomOut(cursorX: Number, cursorY: Number) {
        this.zoom(cursorX, cursorY, -1);
    }

    private getMouseButton(button: number) {
        switch (button) {
            case 0:
                return MouseButton.Left;
            case 1:
                return MouseButton.Middle;
            case 2:
                return MouseButton.Right;
        }
    }

    private prepareEvents() {
        this.outputPane.addEventListener('mousedown', async (ev) => {
            this.connection.sendNotification(this.MouseDownNotification, {button: this.getMouseButton(ev.button), cursorX: ev.clientX, cursorY: ev.clientY});
        });
        this.outputPane.addEventListener('mouseup', (ev) => {
            this.connection.sendNotification(this.MouseUpNotification, {button: this.getMouseButton(ev.button), cursorX: ev.clientX, cursorY: ev.clientY});
        });
        this.outputPane.addEventListener('mousemove', async (ev) => {
            this.connection.sendNotification(this.MouseMoveNotification, {cursorX: ev.clientX, cursorY: ev.clientY});
        });
        this.outputPane.addEventListener('resize', () => {
            // TODO:
        });
        var elements = [
            {id: "panLeftButton", dxm: -1, dym: 0},
            {id: "panRightButton", dxm: 1, dym: 0},
            {id: "panUpButton", dxm: 0, dym: -1},
            {id: "panDownButton", dxm: 0, dym: 1}
        ];
        for (var i = 0; i < elements.length; i++) {
            let element = elements[i];
            (<HTMLButtonElement>document.getElementById(element.id)).addEventListener('click', () => {
                let delta = this.canvas.width / 8;
                this.connection.sendNotification(this.PanNotification, {
                    width: this.canvas.width,
                    height: this.canvas.height,
                    dx: delta * element.dxm,
                    dy: delta * element.dym
                });
            });
        }

        (<HTMLButtonElement>document.getElementById("openButton")).addEventListener('click', async () => {
            var result = await this.connection.sendRequest(this.ExecuteCommandRequest, {command: "File.Open"});
        });

        (<HTMLButtonElement>document.getElementById("zoomInButton")).addEventListener('click', () => {
            let width = this.canvas.clientWidth;
            let height = this.canvas.clientHeight;
            this.zoomIn(width / 2, height / 2);
        });

        (<HTMLButtonElement>document.getElementById("zoomOutButton")).addEventListener('click', () => {
            let width = this.canvas.clientWidth;
            let height = this.canvas.clientHeight;
            this.zoomOut(width / 2, height / 2);
        });

        (<HTMLButtonElement>document.getElementById("openButton")).addEventListener('click', async () => {
            var result = await this.connection.sendRequest(this.ExecuteCommandRequest, {command: "File.Open"});
        });

        this.canvas.addEventListener('wheel', (ev) => {
            this.zoom(ev.offsetX, ev.offsetY, -ev.deltaY);
        });
    }

    private prepareListeners() {
        // notifications
        this.connection.onNotification(this.ClientUpdateNotification, (params) => {
            let clientUpdate = params[0];
            let redraw = false;
            if (clientUpdate.Drawing !== undefined) {
                this.drawing = clientUpdate.Drawing;
                this.populateVertices();
                redraw = true;
            }
            if (clientUpdate.Transform !== undefined) {
                this.transform = clientUpdate.Transform;
                redraw = true;
            }

            if (redraw) {
                this.redraw();
            }
        });

        // file system service
        this.connection.onRequest(new rpc.RequestType<void, string, void, void>('GetFileNameFromUserForOpen'), async () => {
            let result = await remote.dialog.showOpenDialog({
                filters: [
                    { name: 'DXF Files', extensions: ['dxf', 'dxb'] },
                    { name: 'IGES Files', extensions: ['igs', 'iges'] }
                ]
            });

            if (result.filePaths.length > 0) {
                return result.filePaths[0];
            }
            else {
                return null;
            }
        });

        this.connection.onUnhandledNotification((msg) => {
            alert('got unhandled notification ' + msg);
        });

        this.connection.onError((e) => console.log('rpc error: ' + e));
        this.connection.onClose(() => alert('rpc closing'));
    }

    private prepareWebGl() {
        // copypasta begin
        this.gl = this.canvas.getContext("webgl");
        var gl = this.gl;

        var vertCode =
            "attribute vec3 vCoords;\n" +
            "attribute vec3 vColor;\n" +
            "" +
            "uniform mat4 transform;\n" +
            "varying vec4 fColor;\n" +
            "" +
            "void main(void) {\n" +
            "    gl_Position = transform * vec4(vCoords, 1.0);\n" +
            "    fColor = vec4(vColor, 255.0);\n" +
            "}\n";

        var vertShader = gl.createShader(gl.VERTEX_SHADER);
        gl.shaderSource(vertShader, vertCode);
        gl.compileShader(vertShader);

        var fragCode =
            "precision mediump float;\n" +
            "varying vec4 fColor;\n" +
            "void main(void) {\n" +
            "    gl_FragColor = fColor / 255.0;\n" +
            "}\n";
        var fragShader = gl.createShader(gl.FRAGMENT_SHADER);
        gl.shaderSource(fragShader, fragCode);
        gl.compileShader(fragShader);

        var program = gl.createProgram();
        gl.attachShader(program, vertShader);
        gl.attachShader(program, fragShader);
        gl.linkProgram(program);
        gl.useProgram(program);

        this.coordinatesLocation = gl.getAttribLocation(program, "vCoords");
        this.colorLocation = gl.getAttribLocation(program, "vColor");
        this.transformLocation = gl.getUniformLocation(program, "transform");

        // copypasta end
    }

    private populateVertices() {
        var verts = [];
        var cols = [];
        for (var i = 0; i < this.drawing.Lines.length; i++) {
            verts.push(this.drawing.Lines[i].P1.X);
            verts.push(this.drawing.Lines[i].P1.Y);
            verts.push(this.drawing.Lines[i].P1.Z);
            cols.push(this.drawing.Lines[i].Color.R);
            cols.push(this.drawing.Lines[i].Color.G);
            cols.push(this.drawing.Lines[i].Color.B);
            verts.push(this.drawing.Lines[i].P2.X);
            verts.push(this.drawing.Lines[i].P2.Y);
            verts.push(this.drawing.Lines[i].P2.Z);
            cols.push(this.drawing.Lines[i].Color.R);
            cols.push(this.drawing.Lines[i].Color.G);
            cols.push(this.drawing.Lines[i].Color.B);
        }
        var vertices = new Float32Array(verts);
        var colors = new Uint8Array(cols);

        this.bufferSize = vertices.length;

        this.vertexBuffer = this.gl.createBuffer();
        this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.vertexBuffer);
        this.gl.bufferData(this.gl.ARRAY_BUFFER, vertices, this.gl.STATIC_DRAW);

        this.colorBuffer = this.gl.createBuffer();
        this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.colorBuffer);
        this.gl.bufferData(this.gl.ARRAY_BUFFER, colors, this.gl.STATIC_DRAW);

        this.gl.bindBuffer(this.gl.ARRAY_BUFFER, null);
    }

    private redraw() {
        this.gl.viewport(0, 0, this.gl.canvas.width, this.gl.canvas.height);
        //gl.clearColor(0.3921, 0.5843, 0.9294, 1.0); // cornflower blue
        this.gl.clearColor(0.0, 0.0, 0.0, 1.0); // black
        this.gl.clear(this.gl.COLOR_BUFFER_BIT | this.gl.DEPTH_BUFFER_BIT);
        this.gl.uniformMatrix4fv(this.transformLocation, false, this.transform);
        if (this.bufferSize > 0) {
            this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.vertexBuffer);
            this.gl.vertexAttribPointer(this.coordinatesLocation, 3, this.gl.FLOAT, false, 0, 0);
            this.gl.enableVertexAttribArray(this.coordinatesLocation);

            this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.colorBuffer);
            this.gl.vertexAttribPointer(this.colorLocation, 3, this.gl.UNSIGNED_BYTE, false, 0, 0);
            this.gl.enableVertexAttribArray(this.colorLocation);

            this.gl.drawArrays(this.gl.LINES, 0, this.bufferSize / 3);
            this.gl.bindBuffer(this.gl.ARRAY_BUFFER, null);
        }
    }
}
