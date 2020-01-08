using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace SMTPConsole
{
    class Program
    {
        static string Read(Stream stream)
        {
            byte[] buf = new byte[2048];
            int bs = stream.Read(buf, 0, buf.Length);
            string msg = Encoding.Default.GetString(buf, 0, bs);
            Console.Write(msg);
            return msg;
        }

        static string Write(Stream stream, string msg)
        {
            byte[] buf = Encoding.Default.GetBytes(msg);
            stream.Write(buf, 0, buf.Length);
            Console.Write(msg);
            return msg;
        }

		static string Base(string msg)
		{
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(msg));
		}

		static void Send(string server, int port, string src, string dst,
				string passwd, string subj, string msg)
		{
			string user = src.Split('@')[0];
            string res;

            TcpClient tcp = new TcpClient();
            try
            {
                tcp.Connect(server, port);
            }
            catch (SocketException e) {
                Console.WriteLine($"Error connecting to {server}:{port}: {e.Message}");
                return;
            }
            NetworkStream stream = tcp.GetStream();
            SslStream ssl = new SslStream(stream);

            Read(stream);
            Write(stream, "EHLO\r\n");
            Read(stream);

            Write(stream, "STARTTLS\r\n");
            Read(stream);

			ssl.AuthenticateAsClient(server);

            //Write(ssl, "HELO\r\n");
            //Read(ssl);
            Write(ssl, "AUTH LOGIN\r\n");
			Read(ssl);
			Write(ssl, $"{Base(user)}\r\n");
			Read(ssl);
			Write(ssl, $"{Base(passwd)}\r\n");
			res = Read(ssl);

            if (res[0] == '5') goto done;

			Write(ssl, $"MAIL FROM:<{src}>\r\n");
			Read(ssl);
			Write(ssl, $"RCPT TO:<{dst}>\r\n");
			Read(ssl);
            Write(ssl, "DATA\r\n");
            Read(ssl);

			string[] data = {
				$"FROM: {src}",
				$"TO: {dst}",
				$"SUBJECT: {subj}",
				"",
				msg,
				"."
			};
			Write(ssl, $"{string.Join("\r\n", data)}\r\n");
			Read(ssl);
done:
			Write(ssl, "QUIT\r\n");
			Read(ssl);

			ssl.Close();
			stream.Close();
			tcp.Close();
		}

        static string ReadPasswd() {
            ConsoleKeyInfo ch;
            string passwd = "";
            while ((ch = Console.ReadKey(true)).Key != ConsoleKey.Enter) {
                if (ch.Key == ConsoleKey.Backspace) {
                    if (passwd.Length != 0) {
                        passwd = passwd.Substring(0, passwd.Length - 1);
                    }
                    continue;
                }
                passwd = $"{passwd}{ch.KeyChar}";
            }
            Console.WriteLine();
            return passwd;
        }

        static void Main(string[] args)
        {
            string server = args.Length >= 1 ? args[0] : "mail.ngs.ru";
            int port = args.Length >= 2 ? Int32.Parse(args[1]) : 587;
			string src, dst, subj, msg = "";
			string passwd;

            Console.Write("From: ");
            src = Console.ReadLine();
            Console.Write("Password (won't be displayed): ");
            passwd = ReadPasswd();
            Console.Write("To: ");
            dst = Console.ReadLine();
            Console.Write("Subj: ");
            subj = Console.ReadLine();
			Console.WriteLine("\nFinish your message with '.' on a separate line.\n");
			Console.WriteLine("Data:");

			string line = "";
			while ((line = Console.ReadLine()) != ".") {
				msg += $"{line}\r\n";
			}

			Send(server, port, src, dst, passwd, subj, msg);
        }
    }
}
