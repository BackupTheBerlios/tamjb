/// \file
/// $Id$
///
/// A database of information about "playable files". Currently
/// this is only mp3 files, but, well...why constrain yourself?
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
// Contacts:
//
//   Tom Surace <tekhedd@byteheaven.net>

using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.IO;                // Directory functions
using System.Text.RegularExpressions;
using System.Text;

#if USE_SQLITE
using Mono.Data.SqliteClient;
#elif USE_POSTGRESQL
using Npgsql;
#elif USE_MYSQL
using ByteFX.Data;
using ByteFX.Data.MySqlClient;
#endif

///
/// File information database namespace
///

namespace byteheaven.tamjb.Engine
{
   ///
   /// The database class wraps all accesses to our local file database.
   ///
   public class StatusDatabase
   {
      ///
      /// Creates the database wrapper.
      ///
      /// \param dbConnectionString connection string for our database. 
      ///
      public StatusDatabase( string dbConnectionString )
      {
         _Trace( "StatusDatabase" );

         _connectionString = dbConnectionString;

      }

      ~StatusDatabase()
      {
         _Trace( "~StatusDatabase" );
      }

      /// 
      /// Function that creates the necessary tables. If necessary.
      ///
      public void CreateTablesIfNecessary()
      {
         _CreateStatusTable();
         _CreateAttributeTable();
      }

      ///
      /// Create the status table
      ///
      void _CreateStatusTable()
      {
         IDbConnection dbcon = _GetDbConnection();
         IDbCommand cmd = dbcon.CreateCommand();

#if USE_SQLITE
         // Create a table to hold info derived from the files' own 
         // header info:
         string query = 
            "CREATE TABLE file_info ( " +
            "  filekey INTEGER PRIMARY KEY NOT NULL," +
            "  file_path TEXT," +
            "  artist VARCHAR(255)," +
            "  album VARCHAR(255)," +
            "  title VARCHAR(255)," +
            "  track INTEGER," +
            "  genre VARCHAR(80)," +
            "  length_seconds INTEGER" +
            "  )";

#elif USE_MYSQL

         // Create a table to hold info derived from the files' own 
         // header info:
         string query = 
            "CREATE TABLE file_info ( \n" +
            "  filekey INTEGER NOT NULL AUTO_INCREMENT,\n" +
            "  file_path TEXT,\n" +
            "  artist VARCHAR(255),\n" +
            "  album VARCHAR(255),\n" +
            "  title VARCHAR(255),\n" +
            "  track INTEGER,\n" +
            "  genre VARCHAR(80)\n," +
            "  length_seconds INTEGER\n," +
            "  PRIMARY KEY ( filekey )\n" +
            "  ) \n" +
            " TYPE=InnoDB";

#elif USE_POSTGRESQL

         // Create a table to hold info derived from the files' own 
         // header info:
         string query = 
            "CREATE TABLE file_info ( \n" +
            "  filekey SERIAL,\n" +
            "  file_path TEXT,\n" +
            "  artist VARCHAR(255),\n" +
            "  album VARCHAR(255),\n" +
            "  title VARCHAR(255),\n" +
            "  track INTEGER,\n" +
            "  genre VARCHAR(80)\n," +
            "  length_seconds INTEGER\n," +
            "  PRIMARY KEY ( filekey )\n" +
            "  )";

#else
#error No dbtype found
#endif

         cmd.CommandText = query;
         int affected = cmd.ExecuteNonQuery();
         cmd.Dispose();

         dbcon.Close();
         dbcon.Dispose();
      }

      /// 
      /// Function that creates the attribute table
      ///
      /// \todo CREATE INDEX on the attribute table as appropriate
      ///
      void _CreateAttributeTable()
      {
         // Create the Attribute table
         IDbConnection dbcon = _GetDbConnection();
         IDbCommand cmd = dbcon.CreateCommand();

         //   track_ref - reference to file_info.key
         //   playlist_key - index of playlist
         //   value - measure of how appropriate this song is to the
         //     playlist, from 0 to 10000 inclusive. (100.00 percent)
         //
         string query = 
            "CREATE TABLE track_attribute ( " +
            "  track_ref    INTEGER NOT NULL," +
            "  playlist_key INTEGER NOT NULL," +
            "  value        INTEGER NOT NULL" +
            "  )";

         cmd.CommandText = query;
         cmd.ExecuteNonQuery();
         cmd.Dispose();

         // Create an index on the very popular track_ref and playlist_key
         // fields

         cmd = dbcon.CreateCommand();

         // Create a table to hold info derived from the files' own 
         // header info:
         //   statuskey - refers to file_info.key autogenerated unique
         //     btree index
         //   playlist_key - index of playlist
         //   value - measure of how appropriate this song is to the
         //     playlist, from 0 to 10000 inclusive. (100.00 percent)
         //
         query = 
            "CREATE INDEX track_ix ON"
            + " track_attribute ( track_ref )"
            ;

         cmd.CommandText = query;
         cmd.ExecuteNonQuery();
         cmd.Dispose();


         cmd = dbcon.CreateCommand();

         // Create a table to hold info derived from the files' own 
         // header info:
         //   statuskey - refers to file_info.key autogenerated unique
         //     btree index
         //   playlist_key - index of playlist
         //   value - measure of how appropriate this song is to the
         //     playlist, from 0 to 10000 inclusive. (100.00 percent)
         //
         query = 
            "CREATE INDEX playlist_ix ON"
            + " track_attribute ( playlist_key )"
            ;

         cmd.CommandText = query;
         cmd.ExecuteNonQuery();
         cmd.Dispose();

         dbcon.Close();
         dbcon.Dispose();
      }

      ///
      /// Add an entry to the database
      ///
      /// \throw (any) Runtime error on various database failures.
      ///
      /// This does not check for duplicate entries.
      ///
      public void Add( PlayableData newData )
      {
         AddFileInfo( newData );

         /// \todo make a guess at the file's attributes from the ID3 info:
         ///
      }

      public PlayableData GetFileInfo( uint key )
      {
         string query = 
            "SELECT file_path, artist, album, title, track," +
            + " genre, length_seconds" 
            + " FROM file_info"
            + " WHERE filekey = " + key
            ;

         uint value;
         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;
            reader = cmd.ExecuteReader();

            PlayableData returnData;
            if (!reader.Read())
            {
               // Nothing returned. This is bad!
               throw new Exception( "No data returned from query" );
            }
            else
            {
               returnData = new PlayableData();
               returnData.key = key;
               returnData.filePath = reader.GetString(0); // works for TEXT fld
               returnData.artist = reader.GetValue(1).ToString();
               returnData.album = reader.GetValue(2).ToString();
               returnData.title = reader.GetValue(3).ToString();
               returnData.track = reader.GetInt32(4);
               returnData.genre = reader.GetValue(5).ToString();
               returnData.lengthInSeconds = reader.GetInt32(6);
            }

            return returnData;
         }
         catch (Exception e)
         {
            throw new ApplicationException( "Query failed: " + query, e );
         }
         finally
         {
            if (null != reader)
            {
               reader.Close();
               reader.Dispose();
            }

            if (null != cmd)
               cmd.Dispose();

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }
      }

      ///
      /// \note The key field of newData is ignored for obvious reasons
      ///
      void AddFileInfo( PlayableData newData )
      {
         _lastModified = DateTime.Now;

         // Create a table to hold info derived from the files' own 
         // header info:
         string query = 
            "INSERT INTO file_info " +
            " ( " +
            "  file_path," +
            "  artist," +
            "  album," +
            "  title," +
            "  track," +
            "  genre," +
            "  length_seconds" +
            "  )" +
            " VALUES ( " +
            "  '" + _StripEvil( newData.filePath ) + "'," +
            "  '" + _StripEvil( newData.artist ) + "'," +
            "  '" + _StripEvil( newData.album ) + "'," +
            "  '" + _StripEvil( newData.title ) + "'," +
            "  '" + newData.track + "'," +
            "  '" + _StripEvil( newData.genre ) + "'," +
            "  0" +
            "  )";

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
         }
         catch (Exception e)
         {
            // Pass along the exception with the query added
            throw new ApplicationException( "Query failed: " + query, e );
         }
         finally
         {
            if (null != cmd)
               cmd.Dispose();

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }
      }

      // This allows single-tics ("'") because they are filtered separately
      // by tickRegex.
      Regex _invalidCharRegex = new Regex( "[^\\'A-Za-z /!@#$%^&*()-_+=?~]" );
      Regex _tickRegex = new Regex( "'" );

      ///
      /// Removes any special chars from the supplied string, to
      /// avoid cryptic SQL errors.
      ///
      /// Also, escape any single-tick characters.
      ///
      /// \todo Find out why special characters (other than single-tick)
      ///   cause SQLite to
      ///   throw exceptions and replace this regex workaround that
      ///   isn't very friendly to non-english-language users.
      ///
      string _StripEvil( string impureString )
      {
         string firstResult = _invalidCharRegex.Replace( impureString, " " );
         string secondResult = _tickRegex.Replace( firstResult, "''" );

         return secondResult;
      }

      ///
      /// selects one song at random based on the supplied playlist
      /// limitations:
      ///
      /// \param criteria Song seletion criteria (aside from random)
      /// \param key Unique key of the file that is chosen
      ///
      /// \return Total number of tracks in this playlist. If 0, the
      ///   output string track will be empty.
      ///
      public uint PickRandom( ref PlaylistCriteria criteria,
                              out uint   key )
      {
         Debug.Assert( null != criteria ); // parameter required

         key = 0;

         _UpdateTrackCache( ref criteria );

         // 
         // First, how many tracks match this query?
         //
         uint count = criteria.matchCount;
         if (count < 1)
            return count;       // ** quick exit **

         //
         // Pick an entry at random and retrieve it
         //
         uint offset = (uint)_rng.Next( 0, (int)count );

         _Trace( "Choosing track: " + offset );

         string query = 
            _BuildFullQuery( criteria )
            + " LIMIT 1 OFFSET " + offset
            ;
         
         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            _Trace( query );

            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
               // Nothing returned. This is bad!
               throw new Exception( "No data returned from query" );
            }
            else
            {
               key = (uint)reader.GetInt32(0);
            }
         }
         catch (Exception e)
         {
            throw new ApplicationException( "Query failed: " + query, e );
         }
         finally
         {
            if (null != reader)
            {
               reader.Close();
               reader.Dispose();
            }

            if (null != cmd)
               cmd.Dispose();

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }

         return count;
      }

      ///
      /// \return the number of entries in the playlist indexed by
      ///   key.
      ///
      public uint GetRowCount( PlaylistCriteria criteria )
      {
         // Wrap the count query in a count clause, see?
         string query =
            "SELECT count(*) from ( " 
            + _BuildFullQuery( criteria )
            + " ) as count_table";

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            _Trace( query );

            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            // Returns the first parameter, the number of rows:
            object countObj = cmd.ExecuteScalar();
            uint count = Convert.ToUInt32( countObj );

            return count;
         }
         catch (Exception e)
         {
            throw new ApplicationException( "Query failed: " + query, e );
         }
         finally
         {
            if (null != cmd)
               cmd.Dispose();

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }
      }

      string _BuildFullQuery( PlaylistCriteria criteria )
      {
         if (criteria.Count == 0)
         {
            // Just play any file (no limiting criteria)
            return "SELECT filekey FROM file_info";
         }

         StringBuilder query = new StringBuilder( "" );

         bool first = true;
         foreach (DictionaryEntry de in criteria)
         {
            PlaylistCriterion crit = (PlaylistCriterion)de.Value;
            if (!first)
               query.Append( " INTERSECT " );
            else
               first = false;
            
            query.Append( _BuildPartialQuery( crit ) );
         }
         return query.ToString();
      }

      ///
      /// Build one part of a compound SELECT. This means no LIMIT,
      /// ORDER BY, etc will be in the returned query.
      ///
      string _BuildPartialQuery( PlaylistCriterion criterion )
      {
         // Restrict the criteria first, because they'll return the smallest
         // number of rows (smaller than the join anyway).
         string query = 
            "SELECT track_attribute.track_ref"
            + " FROM track_attribute"
            + " WHERE track_attribute.playlist_key = " + criterion.attribKey
            + " AND track_attribute.value >= " + criterion.value
            ;

         /// 
         /// \todo Implment the range checking required to enforce attribute

         return query.ToString();
      }

      ///
      /// A function that imports data from the local-file info table
      /// (file_info) into a playlist's attribute table (track_attribute).
      ///
      /// \param attribKey Unique index of the attribute table
      /// \param initValue Initial attribute value
      ///
      public void ImportNewFiles( uint attribKey, int initValue )
      {
         _lastModified = DateTime.Now;

         // Find every row in file_info for which there is no corresponding
         // entry in track_attribute, and insert the results into 
         // track_attribute.

         string query = 
            "INSERT INTO track_attribute "
            + " ( "
            + " track_ref,"
            + " playlist_key,"
            + " value"
            + " )"
            + " SELECT"
            + " file_info.filekey,"
            + attribKey + ","
            + initValue
            + " FROM file_info"
            + " EXCEPT SELECT "
            + " track_ref,"
            + attribKey + ","
            + initValue
            + " FROM track_attribute "
            + " WHERE track_attribute.playlist_key = " + attribKey
            ;

         _Trace( query );
         _ExecuteNonQuery( query );
      }

      ///
      /// Get the value of the attribute of a particular playlist
      /// for a particular track.
      ///
      public uint GetAttribute( uint playlistKey,
                                uint trackKey )
      {
         string query = 
            "SELECT value FROM track_attribute"
            + " WHERE playlist_key = " + playlistKey
            + " AND track_ref = " + trackKey
            ;

         uint value;
         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
               // Nothing returned. This is bad!
               throw new Exception( "No data returned from query" );
            }
            else
            {
               value = (uint) reader.GetInt32(0);
            }
         }
         catch (Exception e)
         {
            throw new ApplicationException( "Query failed: " + query, e );
         }
         finally
         {
            if (null != reader)
            {
               reader.Close();
               reader.Dispose();
            }

            if (null != cmd)
               cmd.Dispose();

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }

         return value;
      }

      public void SetAttribute( uint playlistKey,
                                uint trackKey,
                                uint newValue )
      {
         string query = 
            "UPDATE track_attribute "
            + " SET value = " + newValue
            + " WHERE playlist_key = " + playlistKey
            + " AND track_ref = " + trackKey
            ;

         _lastModified = DateTime.Now;
         _ExecuteNonQuery( query );
      }

      ///
      /// Check to see if the file indicated by fullPath is already
      /// in the database
      ///
      public bool Mp3FileEntryExists( string fullPath )
      {
         // Create a table to hold info derived from the files' own 
         // header info:
         string query = 
            "SELECT count(*) FROM file_info " +
            "  WHERE file_path = '" + _StripEvil( fullPath ) + "'" +
            "  ;";

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            // Returns the first parameter, the number of rows:
            object countObj = cmd.ExecuteScalar();
            int count = Convert.ToInt32( countObj );

            if (count > 0)
               return true;
         }
         catch (Exception e)
         {
            throw new ApplicationException( "Query failed: " + query, e );
         }
         finally
         {
            if (null != cmd)
               cmd.Dispose();

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }

         return false;
      }

      ///
      /// Cache all track keys in memory to make a lookup table
      /// to make quick selection of random rows possible.
      ///
      void _UpdateTrackCache( ref PlaylistCriteria criteria )
      {
         // For now, just cache the count. could build in-memory
         // ranked tables for better random weights?
         // if (criteria.cacheTime != _lastModified)
         // {
            criteria.matchCount = GetRowCount( criteria );
            criteria.cacheTime = _lastModified;
         // }
      }

      void _ExecuteNonQuery( string sqlString )
      {
         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            _lastModified = DateTime.Now;

            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = sqlString;
            cmd.ExecuteNonQuery();
         }
         catch (Exception e)
         {
            throw new ApplicationException( "Query Failed: " + sqlString, e );
         }
         finally
         {
            if (null != cmd)
               cmd.Dispose();

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }
      }

      ///
      /// Close/Dispose of the returned object, OK?
      ///
      IDbConnection _GetDbConnection()
      {
         // Perhaps the one incredibly asinine thing about .NET: you have
         // to hardcode the type of database you are connecting to. Could
         // this be an intentional mistake? Hah.
#if USE_SQLITE
         IDbConnection dbcon = new SqliteConnection( _connectionString );
#elif USE_POSTGRESQL
         IDbConnection dbcon = new NpgsqlConnection( _connectionString );
#elif USE_MYSQL
         IDbConnection dbcon = new MySqlConnection( _connectionString );
#else
#error No database type found
#endif
         dbcon.Open();
         return dbcon;
      }

      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "StatusDatabase" );
      }


      ///
      /// Time of last database insert or update or whatever. In this
      /// process anyway. :/
      ///
      DateTime _lastModified = DateTime.Now;

      ///
      /// How to connect to our database
      ///
      string _connectionString;

      ///
      /// My local rng
      ///
      Random _rng = new Random();
   }
}

