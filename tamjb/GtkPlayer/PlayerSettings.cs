/// \file
/// $Id$

namespace tam.GtkPlayer
{
   using System;
   using System.Configuration;
   using System.IO;
   using System.Xml.Serialization;

   ///
   /// Settings manipulated by the config dialog and/or stored
   /// in whatever we store our settings in. 
   /// 
   public class PlayerSettings
   {
      /// Name (or ip address) of the server.
      ///
      public string serverName;

      ///
      /// Server port (defaults to, uh, what?)
      public int    serverPort;

      static private string _confBase = 
      System.Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData )
         + "/.tam.GtkPlayerrc";

      public void Store()
      {
         SaveToFile( _confBase );
      }

      static public PlayerSettings Fetch()
      {
         return ReadFromFile( _confBase );
      }

      public void SaveToFile( string name )
      {
         FileStream    stream = null;
         try
         {
            // Create a serializer for the Sailboat type
            XmlSerializer serializer = new XmlSerializer( this.GetType() );

            stream = new FileStream( name,
                                     FileMode.Create,
                                     FileAccess.Write );

            serializer.Serialize( stream, this );
         }
         finally
         {
            if (stream != null)
               stream.Close();
         }
      }

      public static PlayerSettings ReadFromFile( string name )
      {
         FileStream    stream = null;
         try
         {
            // Create a serializer for the Sailboat type
            XmlSerializer serializer = 
               new XmlSerializer( typeof( PlayerSettings ) );

            stream = new FileStream( name,
                                     FileMode.Open,
                                     FileAccess.Read );

            return (PlayerSettings)serializer.Deserialize( stream );
         }
         finally
         {
            if(stream != null)
               stream.Close();
         }
      }
   }
}
