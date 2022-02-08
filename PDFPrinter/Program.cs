using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace port_listen
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            //ClientWorking.ProcessFile(new MemoryStream(File.ReadAllBytes("c:\\temp\\poise.dat")));
            // return;
            Console.WriteLine("Starting...");
            TcpListener server = new TcpListener(IPAddress.Parse("0.0.0.0"), 9100);
            server.Start();
            Console.WriteLine("Started.");
            while (true)
            {
                ClientWorking cw = new ClientWorking(server.AcceptTcpClient());
                new Thread(new ThreadStart(cw.DoSomethingWithClient)).Start();
            }
            server.Stop();
        }
    }

    class ClientWorking
    {
        private Stream ClientStream;
        private TcpClient Client;

        public ClientWorking(TcpClient Client)
        {
            this.Client = Client;
            ClientStream = Client.GetStream();
        }

        static void SavePage(Document doc, PdfPage page, Paragraph para, XGraphics gfx)
        {
            MigraDoc.Rendering.DocumentRenderer docRenderer = new DocumentRenderer(doc);
            docRenderer.PrepareDocument();
            docRenderer.RenderObject(gfx, 20, 40, page.Width, para);
        }
       static Document AddPage(PdfDocument document, out Paragraph para, out PdfPage page, out XGraphics gfx) {
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            gfx.MUH = PdfFontEncoding.Unicode;
            Document doc = new Document();

            Section sec = doc.AddSection();
            // Add a single paragraph with some text and format information.
            para = sec.AddParagraph();
            para.Format.Alignment = ParagraphAlignment.Left;
            para.Format.Font.Name = "Courier New";
            para.Format.Font.Size = 9;
            para.Format.LineSpacingRule = LineSpacingRule.Exactly;
            para.Format.LineSpacing = 8.9;
            
            return doc;
        }
        public static void ProcessFile(MemoryStream ms)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        

            PdfDocument document = new PdfDocument();
            
            Paragraph para;
            PdfPage page;
            XGraphics gfx;
            var doc = ClientWorking.AddPage(document, out para, out page, out gfx);

            



            // Create a renderer and prepare (=layout) the document

            using (ms)
            {
                var r = new System.Text.RegularExpressions.Regex("ID number: ([\\d-]*)");

                ms.Seek(20, SeekOrigin.Begin);

                byte[] buffer = new byte[65];
                ms.Read(buffer, 0, 65);
                var all = Encoding.ASCII.GetString(buffer);
                ms.Seek(-65, SeekOrigin.Current);

                var id = r.Match(all).Groups[1].Value.Replace("-", "");

                for (int i = 0; i < ms.Length -22; i++)
                {
                    byte p = (byte)ms.ReadByte();
                    switch (p)
                    {
                        case 32:

                            para.AddText("\u00A0");
                            break;

                        case 179: // Ver

                            para.AddText("\u2502");
                            break;
                        case 196: // Hoz
                            para.AddText("\u2500");
                            break;
                        case 191: // tRC
                            para.AddText("\u2510");
                            break;
                        case 218: // tLC
                            para.AddText("\u250C");
                            break;
                        case 192: // bLC
                            para.AddText("\u2514");
                            break;
                        case 193: // bottom middle cross
                            para.AddText("\u2534");
                            break;
                        case 194: // Top middle cross
                            para.AddText("\u252c");
                            break;
                        case 195: // left middle cross
                            para.AddText("\u251c");
                            break;
                        case 197: // middle cross
                            para.AddText("\u253c");
                            break;
                        case 180: // right middle cross
                            para.AddText("\u2524");
                            break;
                        case 217: // bRC
                            para.AddText("\u2518");
                            break;
                        case 12:
                            SavePage(doc, page, para, gfx);

                          
                            ms.Read(buffer, 0, 65);
                            all = Encoding.ASCII.GetString(buffer);
                            ms.Seek(-65, SeekOrigin.Current);

                            var newid = r.Match(all).Groups[1].Value.Replace("-", "");
                            if(newid != id)
                            {
                                document.Save($"c:\\temp\\{id}.pdf");
                                id = newid;
                                document = new PdfDocument();
                            }



                            doc = ClientWorking.AddPage(document, out para, out page, out gfx);
                            ms.Seek(2, SeekOrigin.Current);
                            break;
                       
                        default:
                            para.AddText(Encoding.ASCII.GetString(new byte[] { p }));
                            break;


                    }


                }
                SavePage(doc, page, para, gfx);

                document.Save($"c:\\temp\\{id}.pdf");

            }

        }
        public void DoSomethingWithClient()
        {
            using(var ms = new MemoryStream()) {
                Client.GetStream().CopyTo(ms);
                ms.Seek(0,SeekOrigin.Begin);
                ProcessFile(ms);


            }
        }
    }
}