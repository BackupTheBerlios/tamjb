
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
