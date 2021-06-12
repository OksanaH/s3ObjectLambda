using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace TransformXML.Lambda
{    public class Handler
    {
        protected async Task<HttpResponseMessage> Transform(JObject request, ILambdaContext context)
        {
            try
            {                
                LambdaLogger.Log($"Received context { JsonConvert.SerializeObject(context)}");
                LambdaLogger.Log($"Received request  { JsonConvert.SerializeObject(request)}");
                
                var s3Client = new AmazonS3Client();

                var input3Url = request["getObjectContext"]["inputS3Url"].ToString();
                var reqRoute = request["getObjectContext"]["outputRoute"].ToString();
                var token = request["getObjectContext"]["outputToken"].ToString();                

                using var httpClient = new HttpClient();
                var original = await httpClient.GetAsync(input3Url);

                var content = await original.Content.ReadAsStringAsync();
                
                var receivedXml = XDocument.Parse(content);
                var transformedXml = new XElement("article", receivedXml.Root.Element("body").Value);

                var toSend = new WriteGetObjectResponseRequest()
                {
                    Body = ToStream(transformedXml),
                    RequestRoute = reqRoute,
                    RequestToken = token
                };
                var response = await s3Client.WriteGetObjectResponseAsync(toSend);
            }
            catch (Exception ex)
            {
                context.Logger.Log($"ERROR: {ex.Message}; {ex.StackTrace}");              
            }
            return new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK };            
        }

        private Stream ToStream(XElement onlyBodyXML)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(onlyBodyXML.ToString()));
        }
    }
}