/// \file
/// $Id$
///
/// The frontend's settings in a convenient easy-to-save-to-disk
/// object format.
///

// Copyright (C) 2004 Tom Surace.
//
// This file is part of the Tam Jukebox project.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
//   Tom Surace <tekhedd@byteheaven.net>

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
