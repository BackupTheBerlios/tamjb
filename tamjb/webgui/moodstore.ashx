<%@ WebHandler language="C#" Class=byteheaven.tamjb.webgui.MoodStore %>

namespace byteheaven.tamjb.webgui
{
   using System;
   using System.Collections;
   using System.Diagnostics;
   using System.IO;
   using System.Text;
   using System.Web;

   using Jayrock.Json;

   using byteheaven.tamjb.Interfaces;

   public class DojoStoreRequest
   {
      public class QueryOptions
      {
         public bool ignoreCase;
      }

      public string query;
      public QueryOptions queryOptions;
      public IDictionary sort;   // attribute, descending
      public int start;
      public int count;
   }

   ///
   /// Jayrock interface to T.A.M. Jukebox
   ///
   // {
   //    query: {name: "A*"},
   //    queryOptions: {ignoreCase: true},
   //    sort: [{attribute:"name", descending:false}],
   //    start: 0,
   //    count: 10
   // }
   //
   // reply as in:
   // { "identifier" : "id", "numRows" : 30,
   //  "items" : [ { "id" : 10, "login" : "user", "email" : "user@..." }, ... ] }
   //
   public class MoodStore : IHttpHandler
   {
      public bool IsReusable
      { 
         get 
         { 
            return false; 
         } 
      } 

      public void ProcessRequest( HttpContext context ) 
      {
         Console.WriteLine( "[ProcessRequest]" );

         // Read the incoming data and parse it
         Stream stream = context.Request.InputStream;
         long length = stream.Length;
         
         byte[] theBytes = new byte[length];
         stream.Read( theBytes, 0, (int)length );
         string request = Encoding.UTF8.GetString( theBytes, 0, (int)length );

         Console.WriteLine( request );

         // Parse the request:
         

         // string response = "{ "identifier" : "id", "numRows" : 30,
         //  "items" : [ { "id" : 10, "login" : "user", "email" : "user@..." }, ... ] }

         // Write a response suitable for use by the dojo json-based data 
         // store thingy

         TextWriter textWriter = new StringWriter();
         using (JsonWriter writer = new JsonTextWriter(textWriter))
         {
            writer.WriteStartObject();        

            writer.WriteMember( "identifier" ); 
            writer.WriteString( "name" ); 

            writer.WriteMember( "numRows");
            writer.WriteNumber( "1" );

            writer.WriteMember( "items");
            writer.WriteStartArray();
            
            // foreach (Item item 
            // writer.WriteStartObject();
            // ...
            // writer.WriteEndObject();
               
            writer.WriteEndArray();

            writer.WriteEndObject();
         }

         context.Response.ContentType = "text/json";
         context.Response.Write( textWriter.ToString() );
      }
   }
}