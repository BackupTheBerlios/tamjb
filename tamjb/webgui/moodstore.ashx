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

         WebPageBase.Authenticate( out _userID );

         try
         {
            string queryParam = context.Request["queryParam"];
            string startStr = context.Request["start"];
            string countStr = context.Request["count"];
         
            if (null == startStr)
               throw new ArgumentException("start");

            if (null == startStr)
               throw new ArgumentException("count");

            int first = Convert.ToInt32( startStr );
            int count = Convert.ToInt32( countStr );
            int marker = first + count;

            if (first < 0)
               first = 0;
         
            if (marker < first)
               marker = first;

            // 
            // reply as in:
            // { "identifier" : "id", "numRows" : 30,
            //  "items" : [ { "id" : 10, "name" : "MyName"}, ... ] }
            //

            // Get the requested moods, and send a response suitable 
            // for use by the dojo json-based data store thingy

            Mood [] moodArray = WebPageBase.backend.GetMoodList( _userID );
         
            if (first > moodArray.Length)
               first = moodArray.Length;

            if (marker > moodArray.Length)
               marker = moodArray.Length;

            Console.WriteLine( "MoodStore first:{0} marker:{1} length:{2}",
                               first, marker, moodArray.Length );

            TextWriter textWriter = new StringWriter();
            using (JsonWriter writer = new JsonTextWriter(textWriter))
            {
               writer.WriteStartObject();        

               writer.WriteMember( "identifier" ); 
               writer.WriteString( "name" ); 

               writer.WriteMember( "numRows");
               writer.WriteNumber( marker - first );

               writer.WriteMember( "items");
               writer.WriteStartArray();

               for (int i = first; i < marker; i++ )
               {
                  Console.WriteLine( "I:{0}", i );
                  Mood mood = moodArray[i];

                  writer.WriteStartObject();

                  writer.WriteMember( "id" );
                  writer.WriteNumber( mood.id );

                  writer.WriteMember( "name" );
                  writer.WriteString( mood.name );

                  writer.WriteEndObject();
               }
            
               writer.WriteEndArray();

               writer.WriteEndObject();
            }

            context.Response.ContentType = "text/json";
            context.Response.Write( textWriter.ToString() );
         }
         catch (Exception ex)
         {
            Console.WriteLine( "moodstore.ashx: {0}", ex.ToString() );
            throw;
         }
      }

      uint _userID;
   }
}