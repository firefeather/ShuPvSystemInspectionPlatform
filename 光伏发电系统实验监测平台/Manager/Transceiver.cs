﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using Microsoft.VisualBasic.Devices;
using System.Diagnostics;
using System.Threading;

using System.Data;
using System.Data.OleDb;
using 光伏发电系统实验监测平台.Commands;
using 光伏发电系统实验监测平台.Tool;
using 光伏发电系统实验监测平台.Components;
using 光伏发电系统实验监测平台.Database;

namespace 光伏发电系统实验监测平台.Manager
{
	class Transceiver
	{
		SerialPort _serialPort;
		Thread _sendTread;
		Command[] _commands;
		int _cycle;
		Status status;

		const int initComponentId = 6;
		const double initAzimuth = -10;
		const double initObliquity = 22;

		public Transceiver(SerialPort serialPort)
		{
			_serialPort = serialPort;
			_serialPort.DataReceived += DataReceivedHandler;
		}

		public void Start(Command[] commands, int cycle)
		{
			_commands = commands;
			_cycle = cycle;

			status = new Status();
			status.Time = DateTime.Now;
			status.MessageQueue = new List<KeyValuePair<byte, bool>>();
			status.ComponentId = initComponentId;
			status.Azimuth = initAzimuth;
			status.Obliquity = initObliquity;
			OleDbConnection oleDbCon;
			try
			{
				oleDbCon = DatabaseConnection.GetConnection();
				oleDbCon.Open();
			}
			catch (Exception ex)
			{
				throw new Exception("数据库连接失败:" + ex.Message, ex);
			}

			if (_sendTread != null && _sendTread.IsAlive)
				_sendTread.Abort();
			_sendTread = new Thread(new ThreadStart(Work));
			_sendTread.Start();
		}

		public void Stop(List<Command> listCommand)
		{
			if (_sendTread != null && _sendTread.IsAlive)
				_sendTread.Abort();
			if (_serialPort != null && _serialPort.IsOpen)
				_serialPort.Close();
			int sendCount = 3;
			while(sendCount-- > 0)
			{
				_serialPort.BaudRate = 9600;
				_serialPort.Open();
				byte[] bytes = (new Relay32()).GetCommand("停转");
				_serialPort.Write(bytes, 0, bytes.Length);
				_serialPort.Close();
			}


		}

		public void Reset()
		{
			Command[] commands = new Command[4];
			commands[0] = new Command(Command.Operates.打开, 9600);
			commands[1] = new Command(Command.Operates.旋转方位角, (int)initAzimuth);
			commands[2] = new Command(Command.Operates.旋转倾角, (int)initObliquity);
			commands[3] = new Command(Command.Operates.关闭, 0);
			Start(commands, 1);
		}

		private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
		{
			byte[] readbyte = new byte[_serialPort.BytesToRead];

			_serialPort.Read(readbyte, 0, readbyte.Length);
			DateTime dt = DateTime.Now;
			//TODO:在这里写解析过程
		}

		void Work()
		{
			while (_cycle > 0)
			{
				foreach (Command command in _commands)
					switch(command.Operate)
					{
						case Command.Operates.关闭:
							{
								_serialPort.Close();
								break;
							}
						case Command.Operates.打开:
							{
								_serialPort.BaudRate = command.Argument;
								_serialPort.Open();
								break;
							}
						case Command.Operates.旋转倾角:
							{
								//TODO
								break;
							}
						case Command.Operates.旋转方位角:
							{
								//TODO
								break;
							}
						case Command.Operates.查询曲线仪:
							{
								byte[] bytes = (new IV()).GetCommand("查询");
								_serialPort.Write(bytes, 0, bytes.Length);
								break;
							}
						case Command.Operates.查询气象仪:
							{
								byte[] bytes = (new Atmospherium()).GetCommand("查询");
								_serialPort.Write(bytes, 0, bytes.Length);
								break;
							}
						case Command.Operates.等待:
							{
								Thread.Sleep(command.Argument);
								break;
							}
						case Command.Operates.选择组件:
							{
								byte[] bytes = (new Relay8()).GetCommand("组件" + command.Argument);
								_serialPort.Write(bytes, 0, bytes.Length);
								break;
							}
						case Command.Operates.断开组件:
							{
								byte[] bytes = (new Relay8()).GetCommand("断开");
								_serialPort.Write(bytes, 0, bytes.Length);
								break;
							}
						default:
							{
								throw new Exception("不支持的指令");
							}
					}
				--_cycle;
			}
			if (status.OleDbCon != null && status.OleDbCon.State == ConnectionState.Open)
				status.OleDbCon.Close();
		}
	}
}
