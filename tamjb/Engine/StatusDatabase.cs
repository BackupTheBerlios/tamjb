/// \file
/// $Id$
///

// Copyright (C) 2004-2007 Tom Surace.
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

#if USE_FIREBIRD
using FirebirdSql.Data.Firebird;
#elif USE_SQLITE
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
   /// The local file database contains
   /// information about "playable files". Currently
   /// this is only mp3 files, but, well...why constrain yourself?
   ///
   /// This also contains state information used to restore default
   /// settings when the server is stopped and restarted, and so on.
   ///
   public class StatusDatabase
   {
      public enum TrackStatus : int
      {
         OK,
         MISSING
      }

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
         CreateUser( "guest", "" );

         UserInfo guest;
         GetUser( "guest", out guest );

         CreateMood( guest.id, "unknown" );

         // Default settings (will only be UPDATEd from now on)
         string query = 
            "INSERT INTO settings ( \n" +
            "  compression )\n" +
            " VALUES (\n" +
            "  'unknown', ''\n" +
            " )"
            ;

         _ExecuteNonQuery( query );

      }

      ///
      /// Create the status table
      ///
      void _CreateStatusTable()
      {
         try
         {
            _ExecuteNonQuery( "drop table file_info" );
         }
         catch
         {
         }

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
            "  length_seconds INTEGER," +
            "  status INTEGER" +
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
            "  length_seconds INTEGER,\n," +
            "  status INTEGER\n" +
            "  PRIMARY KEY ( filekey )\n" +
            "  ) \n" +
            " TYPE=InnoDB";

#elif USE_POSTGRESQL

         // Create a table to hold info derived from the files' own 
         // header info:
         string query = 
            @"CREATE TABLE file_info (
              filekey SERIAL,
              file_path TEXT,
              artist VARCHAR(255),
              album VARCHAR(255),
              title VARCHAR(255),
              track INTEGER,
              genre VARCHAR(80),
              length_seconds INTEGER,
              status INTEGER,
              PRIMARY KEY ( filekey )
              )";

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
         //   password - yeah we don't even encrypt it

         try
         {
            _ExecuteNonQuery( "drop table users" );
         }
         catch
         {
         }

#if USE_SQLITE
         string query = 
            "CREATE TABLE users ( \n" +
            "  id           INTEGER PRIMARY KEY NOT NULL,\n" +
            "  name         TEXT NOT NULL,\n" +
            "  password     TEXT NOT NULL\n" +
            "  )";
#elif USE_POSTGRESQL
         string query = 
            "CREATE TABLE users ( \n" +
            "  id           SERIAL,\n" +
            "  name         TEXT NOT NULL,\n" +
            "  password     TEXT NOT NULL\n" +
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

         try
         {
            _ExecuteNonQuery( "drop table mood" );
         }
         catch
         {
         }


#if USE_SQLITE
         string query = 
            "CREATE TABLE mood ( \n" +
            "  id           INTEGER PRIMARY KEY NOT NULL,\n" +
            "  user_id      INTEGER NOT NULL,\n" +
            "  name         TEXT NOT NULL\n" +
            "  )";
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
         try
         {
            _ExecuteNonQuery( "drop table song_suck" );
         }
         catch
         {
         }

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
         try
         {
            _ExecuteNonQuery( "drop table song_mood" );
         }
         catch
         {
         }

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
         try
         {
            _ExecuteNonQuery( "drop table settings" );
         }
         catch
         {
         }

         //
         // This table contains one row only, which contains all the
         // application state for restart. Whee.
         //
         // is_playing - 1 if playing, 0 if stopped
         // compression - compression settings (serialized xml)
         //
         string query = 
            "CREATE TABLE user_prefs ( \n" +
            "  userid INTEGER PRIMARY KEY NOT NULL\n" +
            "  prefs  TEXT NOT NULL\n" +
            "  )";

         _ExecuteNonQuery( query );
         query = 
            "CREATE INDEX settings_userid_ix \n" +
            "  ON user_prefs ( userid )";

         _ExecuteNonQuery( query );
      }

      void _CreateUserPrefsTable()
      {
         try
         {
            _ExecuteNonQuery( "drop table user_prefs" );
         }
         catch
         {
         }

         //
         // This table contains one row only, which contains all the
         // application state for restart. Whee.
         //
         // is_playing - 1 if playing, 0 if stopped
         // compression - compression settings (serialized xml)
         //
         string query = 
            "CREATE TABLE user_prefs ( \n" +
            "  compression  TEXT NOT NULL\n" +
            "  )";

         _ExecuteNonQuery( query );
      }

      ///
      /// Store the user's preferences.
      ///
      public void StorePreferences( uint userId, string xmlPrefs )
      {
         Debug.Assert( null != xmlPrefs, "xmlPrefs is required" );

         //
         // Add or update user preferences
         //

         string query = _FixQuery(
            "UPDATE user_prefs"
            + " SET prefs = :prefs"
            + " WHERE userid = :user"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            IDbDataParameter param = _NewParameter( "prefs", DbType.String );
            param.Value = xmlPrefs;
            cmd.Parameters.Add( param );
            
            param = _NewParameter( "user", DbType.Int32 );
            param.Value = userId;
            cmd.Parameters.Add( param );
            
            if (0 >= cmd.ExecuteNonQuery()) // No rows updated. Try insert.
            {
               query = _FixQuery(
                  "INSERT INTO user_prefs ( userid, prefs )"
                  + " VALUES ( :user, :prefs )"
                  );

               cmd.ExecuteNonQuery();
            }
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

      ///
      /// Retrieve the controlling user/mood from the database.
      /// After a restart, this allows you to continue as the same
      /// person. :)
      ///
      /// controller and mood will be null if no current user is
      /// configured. On the other hand, if the controlling user
      /// or mood does not exist, this will throw an exception.
      ///
//       public void GetController( out Credentials controller,
//                                  out Mood mood )
//       {
//          string query = "SELECT control_user, control_mood FROM settings";

//          IDbConnection dbcon = null;
//          IDbCommand cmd = null;
//          IDataReader reader = null;
//          controller = null;     // Default: failure
//          mood = null;
//          try
//          {
//             dbcon = _GetDbConnection();
//             cmd = dbcon.CreateCommand();
//             cmd.CommandText = query;
//             reader = cmd.ExecuteReader();

//             if (!reader.Read())
//             {
//                // Query returned nothing?
//                return; // controller and mood are null. That's OK.
//             }
//             string userName = reader.GetString(0);
//             string moodName = reader.GetString(1);

//             GetUser( userName, out controller );
//             GetMood( controller.id, moodName, out mood );
//          }
//          catch (Exception e)
//          {
//             _Rethrow( query, e );
//          }
//          finally
//          {
//             if (null != reader)
//             {
//                reader.Close();
//                reader.Dispose();
//             }

//             if (null != cmd)
//             {
//                cmd.Dispose();
//             }

//             if (null != dbcon)
//             {
//                dbcon.Close();
//                dbcon.Dispose();
//             }
//          }
//       }


      ///
      ///
      ///
      public void StoreCompressSettings( string xml )
      {
         string query = _FixQuery( 
            "UPDATE settings "
            + " SET compression = :compression"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;


            // Should I be using mono's generic database wrapper?
            IDbDataParameter param = _NewParameter( "compression",
                                                    DbType.String );
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
      public void CreateUser( string name, string password )
      {
         // The default user: guest
         string query = _FixQuery( 
            "INSERT INTO users ("
            + " name,"
            + " password"
            + " ) VALUES ( :newName, :newPass ) "
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            IDbDataParameter param = _NewParameter( "newName",
                                                    DbType.String );
            param.Value = name;
            cmd.Parameters.Add( param );

            param = _NewParameter( "newPass",
                                   DbType.String );
            param.Value = password;
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

      public void CreateMood( uint userId,
                              string name )
      {
         // The default mood: unknown
         string query = _FixQuery( 
            "INSERT INTO mood ( user_id, name ) "
            + "VALUES ( :userId, :userName )"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;


            // Should I be using mono's generic database wrapper?
            IDbDataParameter param = _NewParameter( "userId",
                                                    DbType.Int32 );
            param.Value = userId;
            cmd.Parameters.Add( param );

            IDbDataParameter p2 = _NewParameter( "userName", DbType.String );
            p2.Value = name;
            cmd.Parameters.Add( p2 );

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
      /// Get the list of users in the database
      ///
      /// \return ArrayList full of Credentials objects
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
               returnList.Add( new UserInfo( name, id ) );
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
                           out UserInfo user )
      {
         string query = _FixQuery( 
            "SELECT id"
            + " FROM users"
            + " WHERE name = :name"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            IDbDataParameter param = _NewParameter( "name", DbType.String );
            param.Value = name;
            cmd.Parameters.Add( param );

            reader = cmd.ExecuteReader();

            // PlayableData returnData;
            if (!reader.Read())
            {
               user = null;
               return false;
            }

            uint id = (uint)reader.GetInt32(0); 
            user = new UserInfo( name, id );
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

      public bool Authenticate( UserInfo cred, string password )
      {
         string query = _FixQuery( 
            "SELECT password"
            + " FROM users"
            + " WHERE id = :uid"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            IDbDataParameter param = _NewParameter( "uid", DbType.Int32 );
            param.Value = cred.id;
            cmd.Parameters.Add( param );

            reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
               return false;
            }

            string correctPass = reader.GetString(0);
            return ( password == correctPass );
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
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

      public bool GetUser( uint uid,
                           out UserInfo creds )
      {
         string query = _FixQuery( 
            "SELECT name"
            + " FROM users"
            + " WHERE id = :uid"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            IDbDataParameter param = _NewParameter( "uid", DbType.Int32 );
            param.Value = uid;
            cmd.Parameters.Add( param );

            reader = cmd.ExecuteReader();

            // PlayableData returnData;
            if (!reader.Read())
            {
               creds = null;
               return false;
            }

            string name = reader.GetString(0); 
            creds = new UserInfo( name, uid );
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
      /// \return ArrayList full of Mood objects
      ///
      public ArrayList GetMoodList( uint userId )
      {
         string query = _FixQuery(
            "SELECT id, name"
            + " FROM mood"
            + " WHERE user_id = :userId"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            IDbDataParameter param;
            param = _NewParameter( "userId", DbType.String );
            param.Value = userId;
            cmd.Parameters.Add( param );

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
      public bool GetMood( uint userId,
                           string name,
                           out Mood mood )
      {
         string query = _FixQuery( 
            "SELECT id"
            + " FROM mood"
            + " WHERE name = :userName"
            + " AND user_id = :userId"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            IDbDataParameter param = _NewParameter( "userName", 
                                                    DbType.String );
            param.Value = name;
            cmd.Parameters.Add( param );

            param = _NewParameter( "userId", DbType.String );
            param.Value = userId;
            cmd.Parameters.Add( param );

            reader = cmd.ExecuteReader();

            // PlayableData returnData;
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
      /// Retrieve a user's mood info by id, which is globally unique.
      ///
      /// \return true on success, false if this mood is not found
      ///   
      public bool GetMood( uint id,
                           out Mood mood )
      {
         string query = _FixQuery( 
            "SELECT name"
            + " FROM mood"
            + " WHERE id = :moodId"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            IDbDataParameter param = _NewParameter( "moodId", DbType.String );
            param.Value = id;
            cmd.Parameters.Add( param );

            reader = cmd.ExecuteReader();

            // PlayableData returnData;
            if (!reader.Read()) // no data found?
            {
               mood = null;
               return false;
            }

            string name = reader.GetString(0);
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
            "SELECT file_path, artist, album, title, track,"
            + " genre, length_seconds, status" 
            + " FROM file_info"
            + " WHERE filekey = " + key 
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
               returnData.status = 
                  (byteheaven.tamjb.Interfaces.TrackStatus)reader.GetInt32(7);

               Console.WriteLine( "HHH {0}:{1}", returnData.status, reader.GetInt32(7) );
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
         string query = _FixQuery( 
            @"INSERT INTO file_info
             (
              file_path,
              artist,
              album,
              title,
              track,
              genre,
              length_seconds,
              status
              )
             VALUES ( 
              :filePath,
              :artist,
              :album,
              :title,
              :track,
              :genre,
              0,
              :status
              )"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            IDbDataParameter param;
            param = _NewParameter( "filePath", DbType.String );
            param.Value = newData.filePath;
            cmd.Parameters.Add( param );

            param = _NewParameter( "artist", DbType.String );
            param.Value = newData.artist;
            cmd.Parameters.Add( param );

            param = _NewParameter( "album", DbType.String );
            param.Value = newData.album;
            cmd.Parameters.Add( param );

            param = _NewParameter( "title", DbType.String );
            param.Value = newData.title;
            cmd.Parameters.Add( param );

            param = _NewParameter( "track", DbType.Int32 );
            param.Value = newData.track;
            cmd.Parameters.Add( param );

            param = _NewParameter( "genre", DbType.String );
            param.Value = newData.genre;
            cmd.Parameters.Add( param );

            param = _NewParameter( "status", DbType.Int32 );
            param.Value = (int)newData.status;
            cmd.Parameters.Add( param );

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

      // This allows single-tics ("'") because they are escaped separately
      // by tickRegex. (this is far from inclusive, I know I know...)
      //
      // This is a workaround for SQLITE's complaining. If the SQLITE 
      // wrapper adds support for parameters, we can eliminate this. 
      // Probably.
//       static Regex _invalidCharRegex = 
//          new Regex( "[^\"'\\<>A-Za-z /!@#$%^&*()-_+=?~]" );

//       Regex _tickRegex = new Regex( "'" );

      ///
      /// Removes any special chars from the supplied string, to
      /// avoid cryptic SQL errors.
      ///
      /// Also, escape any single-tick characters.
      ///
//       string _StripEvil( string impureString )
//       {
         // Apparently the postgres wrapper doesn't catch '+'?
//         return impureString.Replace( '+', ' ' );

         // Note: regex seems to cause massive resource leak

         // string firstResult = _invalidCharRegex.Replace( impureString, " " );
         // string secondResult = _tickRegex.Replace( firstResult, "''" );

         // First escape any escape characters in thestring
         // string pure = _invalidCharRegex.Replace( impureString, " " );

//          string pure = impureString.Replace( "\\", "\\\\" );

//          // Second, escape any single ticks
//          pure = pure.Replace( "'", "''" ); 

//          /// \todo Find out why special characters (other than single-tick)
//          ///   cause SQLite to
//          ///   throw exceptions and replace this regex workaround that
//          ///   isn't very friendly to non-english-language users.
//          ///   (Some characters also cause problems on postgres.)
//          ///
//          pure = _tickRegex.Replace( pure, " " );

//          return pure;
//       }

      ///
      /// Chooses a random track without considering mood or suck
      /// 
      /// \return count of tracks in the file_info table
      ///
      public uint PickRandom( Random rng,
                              out PlayableData trackData )
      {
         string query = "";
         trackData = new PlayableData();
         uint count = 0;

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();

            query = "SELECT count(*) FROM file_info"
               + " WHERE status='" + (int)TrackStatus.OK + "'";

            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

            // Returns the first parameter, the number of rows:
            object countObj = cmd.ExecuteScalar();
            count = Convert.ToUInt32( countObj );

            if (count < 1)
               return count;       // ** quick exit **

            //
            // Pick an entry at random and retrieve it
            //
            uint offset = (uint)rng.Next( 0, (int)count );
            
            _Trace( "Choosing track: " + offset );

            query =
               "SELECT file_path, artist, album, title, track,"
               + " genre, length_seconds, filekey" 
               + " FROM file_info"
               + " WHERE status='" + (int)TrackStatus.OK + "'"
               + " LIMIT 1 OFFSET " + offset;
               
            // _Trace( query );

            cmd.Dispose();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;


            reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
               // Nothing returned. This is bad!
               throw new Exception( "No data returned from query" );
            }

            trackData.filePath = reader.GetString(0); // works for TEXT fld
            trackData.artist = reader.GetValue(1).ToString();
            trackData.album = reader.GetValue(2).ToString();
            trackData.title = reader.GetValue(3).ToString();
            trackData.track = reader.GetInt32(4);
            trackData.genre = reader.GetValue(5).ToString();
            trackData.lengthInSeconds = reader.GetInt32(6);
            trackData.key = (uint)reader.GetInt32(7);
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
      /// Chooses the next song from an album.
      /// 
      /// \return data for the next song, or null if not found
      ///   (or if the randomizer says "no")
      ///
      public PlayableData GetTrackByIndex( string artist,
		      			   string album,
		     	                   int index )
      {
         string query = "";

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            dbcon = _GetDbConnection();

            query =
               "SELECT file_path, artist, album, title, track,"
               + " genre, length_seconds, filekey" 
               + " FROM file_info"
               + " WHERE status='" + (int)TrackStatus.OK + "'"
	       + " AND artist=:artist"
	       + " AND album=:album"
	       + " AND track=:index" 
	       ;
               
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;

	    IDbDataParameter param = _NewParameter( "artist", DbType.String );
	    param.Value = artist;
	    cmd.Parameters.Add( param );

	    param = _NewParameter( "album", DbType.String );
	    param.Value = album;
	    cmd.Parameters.Add( param );

	    param = _NewParameter( "index", DbType.Int32 );
	    param.Value = index;
	    cmd.Parameters.Add( param );

            reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
               // Nothing returned. This is bad!
               return null;
            }

	    PlayableData trackData = new PlayableData();
            trackData.filePath = reader.GetString(0); // works for TEXT fld
            trackData.artist = reader.GetValue(1).ToString();
            trackData.album = reader.GetValue(2).ToString();
            trackData.title = reader.GetValue(3).ToString();
            trackData.track = reader.GetInt32(4);
            trackData.genre = reader.GetValue(5).ToString();
            trackData.lengthInSeconds = reader.GetInt32(6);
            trackData.key = (uint)reader.GetInt32(7);

	    return trackData;
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

	 Debug.Assert( false, "not reached" );
	 return null; 
      }


      ///
      /// selects one song at random based on the supplied playlist
      /// limitations:
      ///
      /// \param rng The random number generator to use
      /// \param cred Current user (may be null for no filter)
      /// \param mood That person's mood (may be null for no filter)
      /// \param suckThreshold
      /// \param moodThreshold
      /// \param key Unique key of the file that is chosen
      ///
      /// \return Total number of tracks in this playlist. If 0, the
      ///   output string track will be empty.
      ///
      /// \note This may be broken. I have had it return songs with
      ///   a suck value > the max possible threshold. Hmmm.
      ///
      public uint PickRandom( Random rng,
                              UserInfo cred,
                              Mood mood,
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
         uint offset = (uint)rng.Next( 0, (int)count );

         _Trace( "Choosing track: " + offset );

         string query =  baseQuery  + " LIMIT 1 OFFSET " + offset;
         
         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         IDataReader reader = null;
         try
         {
            // _Trace( query );

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
            // _Trace( query );

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

      string _BuildFullQuery( UserInfo cred,
                              Mood mood,
                              int suckThreshold,
                              int moodThreshold )
      {
         // No filtering? Just select all files
         if (null == cred && null == mood)
            return "SELECT filekey FROM file_info";

         StringBuilder query = new StringBuilder( "" );

         // To INTERSECT both queries, we need a wrapping SELECT for
         // the stupider database engines (SQLite)
         bool needIntersect = (null != cred && null != mood);
         

         if (needIntersect)
            query.Append( "SELECT * FROM (\n" );
         
         // Add user-suck filters
         if (null != cred)
            query.Append( _BuildPartialQuerySuck( cred, suckThreshold ) );

         if (needIntersect)
            query.Append( "\n ) as suck_subquery\n INTERSECT " );
         
         // Add mood filters
         if (null != mood)
            query.Append( _BuildPartialQueryMood( mood, moodThreshold ) );

         return query.ToString();
      }

      ///
      /// Build a subquery that selects based on suck
      ///
      string _BuildPartialQuerySuck( UserInfo cred,
                                     int suckThreshold )
      {
         string query = 
            "SELECT song_suck.track_ref\n"
            + " FROM song_suck\n"
            + " WHERE song_suck.user_id = " + cred.id
            + " AND song_suck.value <= " + suckThreshold
            + " UNION ALL\n"
            + "SELECT file_info.filekey\n"
            + " FROM file_info\n"
            + " WHERE file_info.filekey NOT IN \n"
            + " ( SELECT song_suck.track_ref from song_suck )"
            ;

         return query.ToString();
      }

      ///
      /// Build a subquery that selects based on mood
      ///
      string _BuildPartialQueryMood( Mood mood,
                                     int moodThreshold )
      {
         // Find all the songs that don't suck too much. Note the 
         // outer join--unrated songs are considered to be completely
         // free of suck.

         string query = 
            "SELECT song_mood.track_ref\n"
            + " FROM song_mood\n"
            + " WHERE song_mood.mood_id = " + mood.id + "\n"
            + " AND song_mood.value <= " + moodThreshold + "\n"
            + " UNION ALL\n"
            + "SELECT file_info.filekey\n"
            + " FROM file_info\n"
            + " WHERE file_info.filekey NOT IN \n"
            + " ( SELECT song_mood.track_ref from song_mood )"
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
         // TODO: use parameters here (though this is pretty safe)
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
         string query = _FixQuery(
            "SELECT count(*) FROM file_info"
            + " WHERE file_path = :filePath"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;


            IDbDataParameter param;
            param = _NewParameter( "filePath", DbType.String );
            param.Value = fullPath;
            cmd.Parameters.Add( param );

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

      public void SetTrackStatus( uint key, TrackStatus status )
      {
         string query = _FixQuery( 
            "UPDATE file_info"
            + " SET status=:status"
            + " WHERE filekey=:key"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;


            // Should I be using mono's generic database wrapper?
            IDbDataParameter param = _NewParameter( "status",
                                                    DbType.Int32 );
            param.Value = (int)status;
            cmd.Parameters.Add( param );

            param = _NewParameter( "key", DbType.Int32 );
            param.Value = (int)key;
            cmd.Parameters.Add( param );

            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected < 1)
               throw new ApplicationException( "No rows affected in update" );
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

      public void TrackIsNotMissing( string fullPath, TrackStatus status )
      {
         string query = _FixQuery( 
            "UPDATE file_info"
            + " SET status=:status"
            + " WHERE file_path=:path"
            + " AND status='" + (int)TrackStatus.MISSING + "'"
            );

         IDbConnection dbcon = null;
         IDbCommand cmd = null;
         try
         {
            dbcon = _GetDbConnection();
            cmd = dbcon.CreateCommand();
            cmd.CommandText = query;


            // Should I be using mono's generic database wrapper?
            IDbDataParameter param = _NewParameter( "status",
                                                    DbType.Int32 );
            param.Value = (int)status;
            cmd.Parameters.Add( param );

            param = _NewParameter( "path", DbType.String );
            param.Value = fullPath;
            cmd.Parameters.Add( param );

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
         throw new ApplicationException( "Query failed: " + query, e );
      }

      ///
      /// Close/Dispose of the returned object, OK?
      ///
      IDbConnection _GetDbConnection()
      {
         // Perhaps the one incredibly asinine thing about .NET: you have
         // to hardcode the type of database you are connecting to. Could
         // this be an intentional mistake? Hah.
         try
         {
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
         catch (Exception e)
         {
            throw new ApplicationException( 
               "Problem connecting to database: " + _connectionString, e );
         }
      }

      ///
      /// A very hackish way of supporting postgres-style and modern named
      /// parameters.
      ///
      /// I did say it was hackish, right?
      ///
      string _FixQuery( string atStyleQuery )
      {

// No wait, we don't need to do this for sqlite. Maybe this was for 
// MYSQL? Oh well, who cares!
#if USE_SQLITE

         // return atStyleQuery.Replace( ':', '@' );
         return atStyleQuery;

#elif USE_POSTGRESQL

         return atStyleQuery;   // Nothing to fix

#else
#error not implemented
#endif
      }

      ///
      /// Platform-specific "NewParameter" wrapper function for 
      /// parameteried queries.
      ///
      IDbDataParameter _NewParameter( string name, DbType type )
      {
#if USE_SQLITE
         // I guess they fixed this in sqlite 2 also:
         // return new SqliteParameter( "@" + name, type ) ;
         return new SqliteParameter( name, type ) ;

#elif USE_POSTGRESQL

         return new NpgsqlParameter( name, type );

#else
#error not implemented
#endif
      }


      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "StatusDatabase" );
      }


      ///
      /// How to connect to our database
      ///
      string _connectionString;

      //
      // These are chosen to mostly match the max/min that will be 
      // 100% accepted. For now
      //
      uint _defaultSuckValue        = 01000; // Doesn't suck really
      uint _defaultAppropriateValue = 09000; // Mostly all good
   }
}

