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

using byteheaven.tamjb.Interfaces;

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
         _Trace( "[StatusDatabase]" );

         _connectionString = dbConnectionString;

      }

      ~StatusDatabase()
      {
         _Trace( "[~StatusDatabase]" );
      }

      /// 
      /// Function that creates the necessary tables. If necessary.
      ///
      public void CreateTablesIfNecessary()
      {
         _CreateStatusTable();
         _CreateUsersTable();
         _CreateMoodTable();
         _CreateSongSuckTable();
         _CreateSongMoodTable();
         _CreateSettingsTable();
         
         _CreateInitialData();
      }

      ///
      /// Creates the default user and mood, and other default data
      /// (if any)
      ///
      void _CreateInitialData()
      {
         CreateUser( "guest" );

         ICredentials guest;
         GetUser( "guest", out guest );

         CreateMood( guest, "unknown" );

         // Default settings (will only be UPDATEd from now on)
         string query = 
            "INSERT INTO settings ( \n" +
            "  control_user, control_mood, compression )\n" +
            " VALUES (\n" +
            "  'guest', 'unknown', ''\n" +
            " )"
            ;

         _ExecuteNonQuery( query );

      }

      ///
      /// Create the status table
      ///
      void _CreateStatusTable()
      {
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

         _ExecuteNonQuery( query );
      }

      /// 
      /// Function to create the users table (uh, yeah)
      ///
      void _CreateUsersTable()
      {
         //   id - unique id (autoincrement)
         //   name - string used to log in

#if USE_SQLITE
#error Please implement me
#elif USE_POSTGRESQL
         string query = 
            "CREATE TABLE users ( \n" +
            "  id           SERIAL,\n" +
            "  name         TEXT NOT NULL\n" +
            "  )";
#else
#error No database type found
#endif

         _ExecuteNonQuery( query );
      }

      ///
      /// Create the table for storing mood info (indexing to
      /// user and allowing lookup by name, etc).
      ///
      void _CreateMoodTable()
      {
         //   id - unique id (autoincrement) Unique of all users.
         //   user_id - foreign key ref to users table id (not enforced)
         //     (id of user whose mood this is)
         //   name - Name of this mood

#if USE_SQLITE
#error Please implement me
#elif USE_POSTGRESQL
         string query = 
            "CREATE TABLE mood ( \n" +
            "  id           SERIAL,\n" +
            "  user_id      INTEGER NOT NULL,\n" +
            "  name         TEXT NOT NULL,\n" +
            "  PRIMARY KEY ( id )\n" +
            "  )";
#else
#error No database type found
#endif

         _ExecuteNonQuery( query );
      }

      void _CreateSongSuckTable()
      {
         //
         // Store the suck value of each track on a per-user basis.
         // build indexes on the keys because this is used in joins. A lot.
         //
         string query = 
            "CREATE TABLE song_suck ( \n" +
            "  track_ref    INTEGER NOT NULL,\n" +
            "  user_id      INTEGER NOT NULL,\n" +
            "  value        INTEGER NOT NULL\n" +
            "  )";

         _ExecuteNonQuery( query );

         query = 
            "CREATE INDEX song_suck_track_ref_ix \n" +
            "  ON song_suck ( track_ref )";

         _ExecuteNonQuery( query );

         query = 
            "CREATE INDEX song_suck_user_id_ix \n" +
            "  ON song_suck ( user_id )";

         _ExecuteNonQuery( query );
      }

      void _CreateSongMoodTable()
      {
         //
         // Store the "appropriateness" measurement of each song indexed
         // by (track + user + mood). Yeouch, that's a lot of indexing.
         //
         string query = 
            "CREATE TABLE song_mood ( \n" +
            "  track_ref    INTEGER NOT NULL,\n" +
            "  mood_id      INTEGER NOT NULL,\n" +
            "  value        INTEGER NOT NULL\n" +
            "  )";

         _ExecuteNonQuery( query );

         query = 
            "CREATE INDEX song_mood_track_ref_ix \n" +
            "  ON song_suck ( track_ref )";

         _ExecuteNonQuery( query );

         query = 
            "CREATE INDEX song_mood_mood_id_ix \n" +
            "  ON song_mood ( mood_id )";

         _ExecuteNonQuery( query );
      }

      void _CreateSettingsTable()
      {
         //
         // This table contains one row only, which contains all the
         // application state for restart. Whee.
         //
         // is_playing - 1 if playing, 0 if stopped
         // control_user - controlling user's name (yes, not text)
         // control_mood - controlling user's name (yes, not text)
         // compression - compression settings (serialized xml)
         //
         string query = 
            "CREATE TABLE settings ( \n" +
            "  control_user TEXT NOT NULL,\n" +
            "  control_mood TEXT NOT NULL,\n" +
            "  compression  TEXT NOT NULL\n" +
            "  )";

         _ExecuteNonQuery( query );
      }

      public void StoreController( string user, string mood )
      {
#if USE_POSTGRESQL
         string query = 
            "UPDATE settings "
            + " SET control_user = :user,"
            + "   control_mood = :mood"
            ;
#else
#error I am very lazy, please code this up
#endif

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

#if USE_POSTGRESQL
            IDbDataParameter param = 
               new NpgsqlParameter( "user", DbType.String );
            param.Value = user;
            cmd.Parameters.Add( param );
            
            param = 
               new NpgsqlParameter( "mood", DbType.String );
            param.Value = mood;
            cmd.Parameters.Add( param );
#else
#error I am very lazy, please code this up
#endif
            
            cmd.ExecuteNonQuery();
            return;
         }
         catch (Exception e)
         {
            // Pass along the exception with the query added
            _Rethrow( query, e );
         }
         finally
         {
            if (null != cmd)
            {
               cmd.Dispose();
            }

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }

         throw new ApplicationException( "not reached" );
      }

      ///
      ///
      ///
      public void StoreCompressSettings( string xml )
      {
// Er, let's see, I can write a wrapper class, or use mono-specific
// wrappers, or I can just give up. :)
#if USE_POSTGRESQL
         // Postgres ":variable" instead of the more-common "@variable"
         // Boo!
         string query = 
            "UPDATE settings "
            + " SET compression = :compression"
            ;

         IDbDataParameter param = 
            new NpgsqlParameter( "compression",
                                 DbType.String );
#else
#error I am very lazy, please code this up
#endif


         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            param.Value = xml;
            cmd.Parameters.Add( param );

            cmd.ExecuteNonQuery();
            return;
         }
         catch (Exception e)
         {
            // Pass along the exception with the query added
            _Rethrow( query, e );
         }
         finally
         {
            if (null != cmd)
            {
               cmd.Dispose();
            }

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }

         throw new ApplicationException( "not reached" );
      }

      public string GetCompressSettings()
      {
         string query = "SELECT compression FROM settings";

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
               return null;     // NOT FOUND

            return reader.GetString(0); 
         }
         catch (Exception e)
         {
            _Rethrow( query, e );
         }
         finally
         {
            if (null != reader)
            {
               reader.Close();
               reader.Dispose();
            }

            if (null != cmd)
            {
               cmd.Dispose();
            }

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }

         throw new ApplicationException( "not reached" );
      }

      ///
      /// Create a user with this name. Doesn't give you the ID
      /// or anything, so there!
      ///
      public void CreateUser( string name )
      {
         // The default user: guest
         string query =
            "INSERT INTO users (\n" + 
            "  name\n" +
            " ) VALUES (\n" +
            "  '" + _StripEvil(name) + "'\n" +
            " )";

         _ExecuteNonQuery( query );
      }

      public void CreateMood( ICredentials cred,
                              string name )
      {
         // The default mood: unknown
         string query =
            "INSERT INTO mood (\n" + 
            "  user_id,\n" +
            "  name\n" +
            " ) VALUES (\n" +
            "  " + cred.id + ",\n" +
            "  '" + _StripEvil(name) + "'\n" +
            " )";

         _ExecuteNonQuery( query );
      }

      ///
      /// Get the list of users in the database
      ///
      /// \return ArrayList full of ICredentials objects
      ///
      public ArrayList GetUserList()
      {
         string query = "SELECT id, name FROM users";

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;
            reader = cmd.ExecuteReader();

            ArrayList returnList = new ArrayList();
            while (reader.Read())
            {
               uint id = (uint)reader.GetInt32(0); 
               string name = reader.GetString(1);
               returnList.Add( new Credentials( name, id ) );
            }

            return returnList;
         }
         catch (Exception e)
         {
            _Rethrow( query, e );
         }
         finally
         {
            if (null != reader)
            {
               reader.Close();
               reader.Dispose();
            }

            if (null != cmd)
            {
               cmd.Dispose();
            }

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }

         throw new ApplicationException( "not reached" );
      }

      ///
      /// Retrieve a user's info by name.
      ///
      /// \param name user's name
      /// \param creds Returned as a new user information struct or 
      ///   null if not found. (Check return value)
      ///
      /// \return true on success, false if user does not exist
      ///
      public bool GetUser( string name,
                           out ICredentials creds )
      {
         string query = 
            "SELECT id\n" +
            + " FROM users"
            + " WHERE name = '" + _StripEvil(name) + "'";
            ;

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
               creds = null;
               return false;
            }

            uint id = (uint)reader.GetInt32(0); 
            creds = new Credentials( name, id );
            return true;        // found user, return true!
         }
         catch (Exception e)
         {
            _Rethrow( query, e );
         }
         finally
         {
            if (null != reader)
            {
               reader.Close();
               reader.Dispose();
            }

            if (null != cmd)
            {
               cmd.Dispose();
            }

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }

         throw new ApplicationException( "not reached" );
      }

      ///
      /// Get all moods for this user.
      ///
      /// \return ArrayList full of IMood objects
      ///
      public ArrayList GetMoodList( ICredentials cred )
      {
         string query = 
            "SELECT id, name\n" +
            + " FROM mood\n"
            + " WHERE user_id = " + cred.id;
            ;

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;
            reader = cmd.ExecuteReader();

            ArrayList returnList = new ArrayList();
            while (reader.Read())
            {
               uint id = (uint)reader.GetInt32(0); 
               string name = reader.GetString(1);
               returnList.Add( new Mood( name, id ) );
            }

            return returnList;
         }
         catch (Exception e)
         {
            _Rethrow( query, e );
         }
         finally
         {
            if (null != reader)
            {
               reader.Close();
               reader.Dispose();
            }

            if (null != cmd)
            {
               cmd.Dispose();
            }

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }

         throw new ApplicationException( "not reached" );
      }

      ///
      /// Retrieve a user's mood info by name--name is unique to a 
      /// specific user, although the id is globally unique.
      ///
      /// \return true on success, false if this mood is not found
      ///   
      public bool GetMood( ICredentials credentials,
                           string name,
                           out IMood mood )
      {
         string query = 
            "SELECT id\n" +
            + " FROM mood\n"
            + " WHERE name = '" + _StripEvil(name) + "'\n"
            + " AND user_id = " + credentials.id
            ;

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
            if (!reader.Read()) // no data found?
            {
               mood = null;
               return false;
            }

            uint id = (uint)reader.GetInt32(0); 
            mood = new Mood( name, id );
            return true;        // data found, yay
         }
         catch (Exception e)
         {
            _Rethrow( query, e );
         }
         finally
         {
            if (null != reader)
            {
               reader.Close();
               reader.Dispose();
            }

            if (null != cmd)
            {
               cmd.Dispose();
            }

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }

         throw new ApplicationException( "not reached" );
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
            _Rethrow( query, e );
         }
         finally
         {
            if (null != reader)
            {
               reader.Close();
               reader.Dispose();
            }

            if (null != cmd)
            {
               cmd.Dispose();
            }

            if (null != dbcon)
            {
               dbcon.Close();
               dbcon.Dispose();
            }
         }
         
         throw new ApplicationException( "not reached" );
      }

      ///
      /// \note The key field of newData is ignored for obvious reasons
      ///
      void AddFileInfo( PlayableData newData )
      {
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
            _Rethrow( query, e );
         }
         finally
         {
            if (null != cmd)
            {
               cmd.Dispose();
            }

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
      /// \param cred Current user (may be null for no filter)
      /// \param mood That person's mood (may be null for no filter)
      /// \param suckThreshold
      /// \param moodThreshold
      /// \param key Unique key of the file that is chosen
      ///
      /// \return Total number of tracks in this playlist. If 0, the
      ///   output string track will be empty.
      ///
      public uint PickRandom( ICredentials cred,
                              IMood mood,
                              int suckThreshold,
                              int moodThreshold,
                              out uint   key )
      {
         key = 0;

         string baseQuery = _BuildFullQuery( cred, 
                                             mood, 
                                             suckThreshold, 
                                             moodThreshold );

         // 
         // First, how many tracks match this query?
         //
         uint count = GetRowCount( baseQuery );
         if (count < 1)
            return count;       // ** quick exit **

         //
         // Pick an entry at random and retrieve it
         //
         uint offset = (uint)_rng.Next( 0, (int)count );

         _Trace( "Choosing track: " + offset );

         string query =  baseQuery  + " LIMIT 1 OFFSET " + offset;
         
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
            _Rethrow( query, e );
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
      /// \return the number of entries in the playlist that match
      ///   the criteria
      ///
      public uint GetRowCount( string baseQuery )
      {
         // Wrap the count query in a count clause, see?
         string query =
            "SELECT count(*) from ( " + baseQuery + " ) as count_table";

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
            _Rethrow( query, e );
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

         throw new ApplicationException( "not reached" );
     }

      string _BuildFullQuery( ICredentials cred,
                              IMood mood,
                              int suckThreshold,
                              int moodThreshold )
      {
         // No filtering? Just select all files
         if (null == cred && null == mood)
            return "SELECT filekey FROM file_info";

         StringBuilder query = new StringBuilder( "" );

         // Add user-suck filters
         if (null != cred)
            query.Append( _BuildPartialQuerySuck( cred, suckThreshold ) );

         if (null != cred && null != mood)
            query.Append( " INTERSECT " );
         
         // Add mood filters
         if (null != mood)
            query.Append( _BuildPartialQueryMood( mood, moodThreshold ) );

         return query.ToString();
      }

      ///
      /// Build a subquery that selects based on suck
      ///
      string _BuildPartialQuerySuck( ICredentials cred,
                                     int suckThreshold )
      {
         string query = 
            "( SELECT song_suck.track_ref\n"
            + " FROM song_suck\n"
            + " WHERE song_suck.user_id = " + cred.id
            + " AND song_suck.value <= " + suckThreshold
            + " UNION ALL\n"
            + "SELECT file_info.filekey\n"
            + " FROM file_info\n"
            + " WHERE file_info.filekey NOT IN \n"
            + " ( SELECT song_suck.track_ref from song_suck ) )"
            ;

         return query.ToString();
      }

      ///
      /// Build a subquery that selects based on mood
      ///
      string _BuildPartialQueryMood( IMood mood,
                                     int moodThreshold )
      {
         // Find all the songs that don't suck too much. Note the 
         // outer join--unrated songs are considered to be completely
         // free of suck.

         string query = 
            "( SELECT song_mood.track_ref\n"
            + " FROM song_mood\n"
            + " WHERE song_mood.mood_id = " + mood.id + "\n"
            + " AND song_mood.value <= " + moodThreshold + "\n"
            + " UNION ALL\n"
            + "SELECT file_info.filekey\n"
            + " FROM file_info\n"
            + " WHERE file_info.filekey NOT IN \n"
            + " ( SELECT song_mood.track_ref from song_mood ) )"
            ;

         return query.ToString();
      }

      ///
      /// Get the suck value of a track
      ///
      /// If the suck metric does not exist, it is added!
      ///
      public uint GetSuck( uint userId, uint trackKey )
      {
         string query = 
            "SELECT value FROM song_suck"
            + " WHERE track_ref = " + trackKey
            + " AND user_id = " + userId
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
               // This song is not rated (NR? :), so just return the
               // default value for it:
               value = _defaultSuckValue;
            }
            else
            {
               value = (uint) reader.GetInt32(0);
            }
         }
         catch (Exception e)
         {
            _Rethrow( query, e );
            value = 0;
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

      ///
      /// Set the suck value for a given song (according to this user's
      /// opinion)
      ///
      public void SetSuck( uint userId, uint trackKey, uint newLevel )
      {
         string query = 
            "UPDATE song_suck "
            + " SET value = " + newLevel
            + " WHERE track_ref = " + trackKey
            + " AND user_id = " + userId
            ;

         if (0 >= _ExecuteNonQuery( query ))
         {
            // No rows affected, try insert?
            query = 
               "INSERT INTO song_suck ( value, track_ref, user_id )" +
               " VALUES ( \n" +
               newLevel + ",\n" +
               trackKey + ",\n" +
               userId +
               " )";
               
            _ExecuteNonQuery( query );
         }
      }


      ///
      /// Get the mood-appropriateness value of a track
      ///
      public uint GetAppropriate( uint userId, uint moodId, uint trackKey )
      {
         string query = 
            "SELECT value FROM song_mood"
            + " WHERE track_ref = " + trackKey
            + " AND mood_id = " + moodId
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
               // Nothing returned. use default
               return _defaultAppropriateValue;
            }
            else
            {
               value = (uint) reader.GetInt32(0);
            }
         }
         catch (Exception e)
         {
            _Rethrow( query, e );
            value = 0;
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

      ///
      /// Set the mood-appropriateness value of a track
      ///
      public void SetAppropriate( uint userId, 
                                  uint moodId,
                                  uint trackKey, 
                                  uint newLevel )
      {
         string query = 
            "UPDATE song_mood "
            + " SET value = " + newLevel
            + " WHERE track_ref = " + trackKey
            + " AND mood_id = " + moodId
            ;

         if (0 >= _ExecuteNonQuery( query ))
         {
            // No rows affected, try insert?
            query = 
               "INSERT INTO song_mood ( value, track_ref, mood_id )" +
               " VALUES ( \n" +
               newLevel + ",\n" +
               trackKey + ",\n" +
               moodId +
               " )";
               
            _ExecuteNonQuery( query );
         }
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
            _Rethrow( query, e );
            value = 0;
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
            _Rethrow( query, e );
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
      /// \return the number of rows affected (UPDATE INSERT DELETE),
      ///   just like IDbCommand.ExecuteNonQuery.
      ///
      int _ExecuteNonQuery( string sqlString )
      {
         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = sqlString;
            return cmd.ExecuteNonQuery();
         }
         catch (Exception e)
         {
            _Rethrow( sqlString, e );
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

         throw new ApplicationException( "not reached" );
      }

      ///
      /// Rethrow a caught exception, adding the query.
      ///
      void _Rethrow( string query, Exception e )
      {
#if USE_POSTGRESQL
         // Avoid throwing non-serializable NpgsqlException as 
         // InnerException
         throw new ApplicationException( "Query failed: " + query
                                         + "-->" + e.ToString() );
#else
         throw new ApplicationException( "Query failed: " + query, e );
#endif
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
      /// How to connect to our database
      ///
      string _connectionString;

      ///
      /// My local rng
      ///
      Random _rng = new Random();

      uint _defaultSuckValue        = 00000; // Doesn't suck AT ALL
      uint _defaultAppropriateValue = 10000; // "It's all good" (TM)
   }
}

