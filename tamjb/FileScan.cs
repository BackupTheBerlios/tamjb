/// \file
/// $Id$
///
/// An obsolete standalone mp3 file scanning program used in prototyping.
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

using System;
using System.Collections;
using System.Data;
using System.IO;                
using tam.LocalFileDatabase;
using Mono.Data.SqliteClient;

public class FileScan
{
   static void _Usage()
   {
      Console.WriteLine( "usage: FileScan <--create> <--dburl=file:/my/file.db> [dirs to scan]" );
   }

   public static void Main( string[] args )
   {
      try
      {
         bool createTables = false;
         ArrayList dirs = new ArrayList();

         string connectionString = "URI=file:audio_filez.db";
         foreach (string arg in args)
         {
            if (arg.StartsWith( "-" ))
            {
               string [] parts = arg.Split( new char[] { '=' } );
               if (parts[0] == "--create") // create db?
               {
                  createTables = true;
               }
               else if (parts[0].StartsWith("--dburl"))
               {
                  if (parts.Length < 2)
                  {
                     _Usage();
                     return;
                  }
                  connectionString = "URI=" + parts[1];
               }
               else
               {
                  _Usage();
                  return;
               }
            }
            else
            {
               dirs.Add( arg );
            }
         }

         // Perhaps the one incredibly asinine thing about .NET: you have
         // to hardcode the type of database you are connecting to. Could
         // this be an intentional mistake?
         IDbConnection dbcon = new SqliteConnection(connectionString);
         dbcon.Open();
            
         StatusDatabase db = new StatusDatabase( dbcon );
         
         if (createTables)
            db.CreateTablesIfNecessary();

         foreach (string rootDir in dirs)
         {
            Console.WriteLine( "Scanning '" + rootDir + "'" );
      
            // Find some files
            db.Scan( rootDir );
         }            

         dbcon.Close();         // Only after all other objects are done

         Console.WriteLine( "Done" );
      }
      catch (Exception e)
      {
         Console.WriteLine( e.ToString() );
      }
   }

}
