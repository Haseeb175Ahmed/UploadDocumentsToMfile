using MFaaP.MFWSClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace UploadDocumentsToMfile.Models
{
    public class Mfile
    {
        public static string domain = "http://127.0.0.1/";

        public static string AuthToken()
        {
            string Token = "";
            try
            {
                var jsonSerializer = JsonSerializer.CreateDefault();

                var auth = new
                {
                    Username = "Admin",
                    Password = "123",
                    VaultGuid = "{E30EEB1E-891D-4BC8-B171-218DCCBE9035}" // Use GUID format with {braces}.
                };
                string url = "http://127.0.0.1/REST/server/authenticationtokens.aspx";
                var authenticationRequest = (HttpWebRequest)WebRequest.Create(url);
                authenticationRequest.Method = "POST";

                // Add the authentication details to the request stream.
                using (var streamWriter = new StreamWriter(authenticationRequest.GetRequestStream()))
                {
                    using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                    {
                        jsonSerializer.Serialize(jsonTextWriter, auth);
                    }
                }

                // Execute the request.
                var authenticationResponse = (HttpWebResponse)authenticationRequest.GetResponse();

                // Extract the authentication token.
                string authenticationToken = null;
                using (var streamReader = new StreamReader(authenticationResponse.GetResponseStream()))
                {
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        authenticationToken = ((dynamic)jsonSerializer.Deserialize(jsonTextReader)).Value;
                        Token = authenticationToken;
                    }
                }

            }
            catch (Exception ex)
            {

               

            }
            return Token;
        }

        public static async Task<ObjectVersion> UploadDocumnet(string FileName, string keyword,Stream filestream,string extension)
        {
            ObjectVersion objectVersion = new ObjectVersion();
            try
            {

             
                var client = new System.Net.Http.HttpClient();

                // Authenticate.
                client.DefaultRequestHeaders.Add("X-Authentication", AuthToken());


                var objectCreationInfo = new ObjectCreationInfo()
                {
                    PropertyValues = new[]
                    {
                        new PropertyValue()
                        {
                            PropertyDef = 100, // The built-in "Class" property Id.
			                TypedValue = new TypedValue()
                            {
                                DataType = MFDataType.Lookup,
                                Lookup = new Lookup()
                                {
                                    Item = 0, // The built-in "Document" class Id.
					                Version = -1 // Work around the bug detailed below.
				                }
                            }
                        },
                        new PropertyValue()
                        {
                            PropertyDef = 0, // The built-in "Name or Title" property Id.
			                TypedValue = new TypedValue()
                            {
                                DataType = MFDataType.Text,
                                Value = FileName
                            }
                        },
                        new PropertyValue()
                        {
                            PropertyDef = 1004, // The built-in "Keyword" property Id.
			                TypedValue = new TypedValue()
                            {
                                DataType = MFDataType.Text,
                                Value = keyword
                            }
                        }
                    }
                };



                
                // NOTE: http://developer.m-files.com/APIs/REST-API/#iis-compatibility
                var uploadFileResponse = await client.PostAsync(new Uri("http://127.0.0.1/REST/files.aspx"),
                    new System.Net.Http.StreamContent(filestream)).ConfigureAwait(false);

                // Extract the value.
                var uploadInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<UploadInfo>(
                    await uploadFileResponse.Content.ReadAsStringAsync().ConfigureAwait(false));

                // Ensure the extension is set.
                // NOTE: This must be without the dot!
                uploadInfo.Extension = extension.Replace(".","");

                // Add the upload to the objectCreationInfo.
                objectCreationInfo.Files = new[] { uploadInfo };

                // What type of object are we creating?
                const int documentObjectTypeId = 0;

                // Execute the post.
                // NOTE: http://developer.m-files.com/APIs/REST-API/#iis-compatibility
                var createObjectResponse = await client.PostAsync(new Uri("http://127.0.0.1/REST/objects/" + documentObjectTypeId + ".aspx"),
                    new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(objectCreationInfo), Encoding.UTF8, "application/json")).ConfigureAwait(false);

                // Extract the value.
                var objectVersion1 = Newtonsoft.Json.JsonConvert.DeserializeObject<ObjectVersion>(
                    await createObjectResponse.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {



            }

            return objectVersion;
        }

       

    }
}