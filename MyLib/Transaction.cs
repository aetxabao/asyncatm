using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace MyLib
{

    public class Transaction
    {
        public string From { get; set; }
        public string To { get; set; }
        public double Amount { get; set; }
        public string Comment { get; set; }
        public DateTime Stamp { get; set; }
        public bool Status { get; set; }

        public Transaction()
        {
            this.From = "";
            this.To = "";
            this.Amount = 0.0;
            this.Comment = "";
            this.Stamp = DateTime.Now;
            this.Status = false;
        }
        public Transaction(string from, string to, double amount, string comment)
        {
            this.From = from;
            this.To = to;
            this.Amount = amount;
            this.Comment = comment;
            this.Stamp = DateTime.Now;
            this.Status = false;
        }
        public static Transaction FromXml(string xml)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(xml);
            MemoryStream stream = new MemoryStream(byteArray);
            Transaction t = (Transaction)new XmlSerializer(typeof(Transaction)).Deserialize(stream);
            return t;
        }
        public byte[] ToByteArray()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Transaction));
            Stream stream = new MemoryStream();
            xmlSerializer.Serialize(stream, this);
            byte[] byteData = ((MemoryStream)stream).ToArray();
            return byteData;
        }

        public override string ToString()
        {
            return $"from: {From}, to: {To}, amount: {Amount.ToString("0.##")}, comment: {Comment}, stamp: {Stamp.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}";
        }

    }

}
