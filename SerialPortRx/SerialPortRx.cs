﻿namespace CP.IO.Ports
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO.Ports;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using Reactive.Bindings;
    using Reactive.Bindings.Extensions;

    /// <summary>
    /// Serial Port Rx
    /// </summary>
    /// <seealso cref="System.IDisposable"/>
    public class SerialPortRx : IDisposable, ISerialPortRx
    {
        private ISubject<char> dataReceived = new Subject<char>();
        private IDisposable disposablePort;
        private ISubject<Exception> errors = new Subject<Exception>();
        private ISubject<Tuple<byte[], int, int>> writeByte = new Subject<Tuple<byte[], int, int>>();
        private ISubject<Tuple<char[], int, int>> writeChar = new Subject<Tuple<char[], int, int>>();
        private ISubject<string> writeString = new Subject<string>();
        private ISubject<string> writeStringLine = new Subject<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortRx"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="dataBits">The data bits.</param>
        /// <param name="parity">The parity.</param>
        /// <param name="stopBits">The stop bits.</param>
        /// <param name="handshake">The handshake.</param>
        public SerialPortRx(string port, int baudRate, int dataBits, Parity parity, StopBits stopBits, Handshake handshake)
        {
            this.PortName = port;
            this.BaudRate = baudRate;
            this.DataBits = dataBits;
            this.Parity = parity;
            this.StopBits = stopBits;
            this.Handshake = handshake;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortRx"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="dataBits">The data bits.</param>
        /// <param name="parity">The parity.</param>
        /// <param name="stopBits">The stop bits.</param>
        public SerialPortRx(string port, int baudRate, int dataBits, Parity parity, StopBits stopBits)
        {
            this.PortName = port;
            this.BaudRate = baudRate;
            this.DataBits = dataBits;
            this.Parity = parity;
            this.StopBits = stopBits;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortRx"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="dataBits">The data bits.</param>
        /// <param name="parity">The parity.</param>
        public SerialPortRx(string port, int baudRate, int dataBits, Parity parity)
        {
            this.PortName = port;
            this.BaudRate = baudRate;
            this.DataBits = dataBits;
            this.Parity = parity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortRx"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="dataBits">The data bits.</param>
        public SerialPortRx(string port, int baudRate, int dataBits)
        {
            this.PortName = port;
            this.BaudRate = baudRate;
            this.DataBits = dataBits;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortRx"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="baudRate">The baud rate.</param>
        public SerialPortRx(string port, int baudRate)
        {
            this.PortName = port;
            this.BaudRate = baudRate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortRx"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        public SerialPortRx(string port)
        {
            this.PortName = port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortRx"/> class.
        /// </summary>
        public SerialPortRx()
        {
        }

        /// <summary>
        /// Gets or sets the baud rate.
        /// </summary>
        /// <value>The baud rate.</value>
        [Browsable(true)]
        [DefaultValue(9600)]
        [MonitoringDescription("BaudRate")]
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// Gets or sets the data bits.
        /// </summary>
        /// <value>The data bits.</value>
        [Browsable(true)]
        [DefaultValue(8)]
        [MonitoringDescription("DataBits")]
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// Gets the data received.
        /// </summary>
        /// <value>The data received.</value>
        public IObservable<char> DataReceived => dataReceived.AsObservable().Retry().Publish().RefCount();

        /// <summary>
        /// Gets the error received.
        /// </summary>
        /// <value>The error received.</value>
        public IObservable<Exception> ErrorReceived => errors.AsObservable().Distinct(ex => ex.Message).Retry().Publish().RefCount();

        /// <summary>
        /// Gets or sets the handshake.
        /// </summary>
        /// <value>The handshake.</value>
        [Browsable(true)]
        [DefaultValue(Handshake.None)]
        [MonitoringDescription("Handshake")]
        public Handshake Handshake { get; set; } = Handshake.None;

        /// <summary>
        /// Gets the is open.
        /// </summary>
        /// <value>The is open.</value>
        [Browsable(true)]
        [MonitoringDescription("IsOpen")]
        public IReadOnlyReactiveProperty<bool> IsOpen => isOpen.ToReadOnlyReactiveProperty();

        /// <summary>
        /// Gets or sets the parity.
        /// </summary>
        /// <value>The parity.</value>
        [Browsable(true)]
        [DefaultValue(Parity.None)]
        [MonitoringDescription("Parity")]
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        [Browsable(true)]
        [DefaultValue("COM1")]
        [MonitoringDescription("PortName")]
        public string PortName { get; set; } = "COM1";

        /// <summary>
        /// Gets the port names.
        /// </summary>
        /// <value>The port names.</value>
        public IObservable<string[]> PortNames => Observable.Create<string[]>(obs =>
        {
            string[] compare = null;
            return Observable.Interval(TimeSpan.FromMilliseconds(500)).Subscribe(_ =>
             {
                 var compareNew = SerialPort.GetPortNames();
                 if (compareNew.Length == 0)
                     compareNew = new string[] { "NoPorts" };
                 if (compare == null)
                 {
                     compare = compareNew;
                     obs.OnNext(compareNew);
                 }
                 if (string.Join("", compare) != string.Join("", compareNew))
                 {
                     obs.OnNext(compareNew);
                     compare = compareNew;
                 }
             });
        }).Retry().Publish().RefCount();

        /// <summary>
        /// Gets or sets the read timeout.
        /// </summary>
        /// <value>The read timeout.</value>
        [Browsable(true)]
        [DefaultValue(-1)]
        [MonitoringDescription("ReadTimeout")]
        public int ReadTimeout { get; set; } = -1;

        /// <summary>
        /// Gets or sets the stop bits.
        /// </summary>
        /// <value>The stop bits.</value>
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>
        /// Gets or sets the write timeout.
        /// </summary>
        /// <value>The write timeout.</value>
        [Browsable(true)]
        [DefaultValue(-1)]
        [MonitoringDescription("WriteTimeout")]
        public int WriteTimeout { get; set; } = -1;

        private IObservable<Unit> Connect => Observable.Create<Unit>(obs =>
        {
            var dis = new CompositeDisposable();

            // Check that the port exists
            if (!SerialPort.GetPortNames().Any(name => name.Equals(this.PortName)))
            {
                obs.OnError(new Exception($"Serial Port {this.PortName} does not exist"));
            }
            else
            {
                // Setup Com Port
                var port = new SerialPort(this.PortName, this.BaudRate, this.Parity, this.DataBits, this.StopBits);
                port.AddTo(dis);
                port.Handshake = this.Handshake;
                port.ReadTimeout = this.ReadTimeout;
                port.WriteTimeout = this.WriteTimeout;
                port.Open();
                isOpen.Value = port.IsOpen;

                // Clear any existing buffers
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                Thread.Sleep(100);

                // Subscribe to port errors
                port.ErrorReceivedObserver().Subscribe(e => obs.OnError(new Exception(e.EventArgs.EventType.ToString()))).AddTo(dis);

                // Get the stream of Char from the serial port
                var dataStream =
                    from dataRecieved in port.DataReceivedObserver()
                    from data in port.ReadExisting()
                    select data;
                dataStream.Subscribe(dataReceived.OnNext, obs.OnError).AddTo(dis);

                // setup Write streams
                writeString.Subscribe(x =>
                {
                    try { port.Write(x); }
                    catch (Exception ex)
                    {
                        obs.OnError(ex);
                    }
                }, obs.OnError).AddTo(dis);
                writeStringLine.Subscribe(x =>
                {
                    try { port.WriteLine(x); }
                    catch (Exception ex)
                    {
                        obs.OnError(ex);
                    }
                }, obs.OnError).AddTo(dis);
                writeByte.Subscribe(x =>
                {
                    try { port.Write(x.Item1, x.Item2, x.Item3); }
                    catch (Exception ex)
                    {
                        obs.OnError(ex);
                    }
                }, obs.OnError).AddTo(dis);
                writeChar.Subscribe(x =>
                {
                    try { port.Write(x.Item1, x.Item2, x.Item3); }
                    catch (Exception ex)
                    {
                        obs.OnError(ex);
                    }
                }, obs.OnError).AddTo(dis);
            }
            return Disposable.Create(() =>
            {
                this.isOpen.Value = false;
                dis.Dispose();
            });
        }).OnErrorRetry((Exception ex) => errors.OnNext(ex)).Publish().RefCount();

        private IReactiveProperty<bool> isOpen { get; } = new ReactiveProperty<bool>();

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            disposablePort?.Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.disposablePort.Dispose();
        }

        /// <summary>
        /// Opens this instance.
        /// </summary>
        public void Open()
        {
            Task.Run(() => disposablePort = Connect.Subscribe());
        }

        /// <summary>
        /// Writes the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void Write(string text)
        {
            this.writeString?.OnNext(text);
        }

        /// <summary>
        /// Writes the specified byte array.
        /// </summary>
        /// <param name="byteArray">The byte array.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public void Write(byte[] byteArray, int offset, int count)
        {
            this.writeByte?.OnNext(new Tuple<byte[], int, int>(byteArray, offset, count));
        }

        /// <summary>
        /// Writes the specified byte array.
        /// </summary>
        /// <param name="byteArray">The byte array.</param>
        public void Write(byte[] byteArray)
        {
            this.writeByte?.OnNext(new Tuple<byte[], int, int>(byteArray, 0, byteArray.Length));
        }

        /// <summary>
        /// Writes the specified character array.
        /// </summary>
        /// <param name="charArray">The character array.</param>
        public void Write(char[] charArray)
        {
            this.writeChar?.OnNext(new Tuple<char[], int, int>(charArray, 0, charArray.Length));
        }

        /// <summary>
        /// Writes the specified character array.
        /// </summary>
        /// <param name="charArray">The character array.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public void Write(char[] charArray, int offset, int count)
        {
            this.writeChar?.OnNext(new Tuple<char[], int, int>(charArray, offset, count));
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="text">The text.</param>
        public void WriteLine(string text)
        {
            this.writeStringLine?.OnNext(text);
        }
    }
}