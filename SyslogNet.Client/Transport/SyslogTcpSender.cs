using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using SyslogNet.Client.Serialization;

namespace SyslogNet.Client.Transport
{
	public enum MessageTransfer {
		OctetCounting		= 0,
		NonTransparentFraming	= 1
	}

	public class SyslogTcpSender : ISyslogMessageSender, IDisposable
	{
		protected readonly TcpClient tcpClient;
		protected Stream transportStream;

		public MessageTransfer messageTransfer { get; set; }
		public byte trailer { get; set; }

		public SyslogTcpSender(string hostname, int port)
		{
			try
			{
				tcpClient = new TcpClient(hostname, port);
				transportStream = tcpClient.GetStream();

				messageTransfer = MessageTransfer.OctetCounting;
				trailer = 10; // LF
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public void Send(SyslogMessage message, ISyslogMessageSerializer serializer)
		{
			Send(message, serializer, true);
		}

		protected void Send(SyslogMessage message, ISyslogMessageSerializer serializer, bool flush = true)
		{
			var datagramBytes = serializer.Serialize(message);

			if (messageTransfer.Equals(MessageTransfer.OctetCounting))
			{
				byte[] messageLength = Encoding.ASCII.GetBytes(datagramBytes.Length.ToString());
				transportStream.Write(messageLength, 0, messageLength.Length);
				transportStream.WriteByte(32); // Space
			}

			transportStream.Write(datagramBytes, 0, datagramBytes.Length);

			if (messageTransfer.Equals(MessageTransfer.NonTransparentFraming))
			{
				transportStream.WriteByte(trailer); // LF
			}

			if (flush && !(transportStream is NetworkStream))
				transportStream.Flush();
		}

		public void Send(IEnumerable<SyslogMessage> messages, ISyslogMessageSerializer serializer)
		{
			foreach (SyslogMessage message in messages)
			{
				Send(message, serializer, false);
			}

			if (!(transportStream is NetworkStream))
				transportStream.Flush();
		}

		public void Dispose()
		{
			if (transportStream != null)
				transportStream.Close();

			if (tcpClient != null)
				tcpClient.Close();
		}
	}
}