/// \file
/// $Id$
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
// Feel free to track down and contact ALL project contributors to
// negotiate other terms. Bring a checkbook.
//
//   Tom Surace <tekhedd@byteheaven.net>

namespace tam.LocalFileDatabase
{
   using System;
   using System.Collections;
   using System.Collections.Specialized;
   using System.Diagnostics;

   /// 
   /// Criteria for inclusion in a playlist.
   ///
   /// \note The contained collection (returned by foreach) is
   ///   a DictionaryEntry, with the key a uint and the value
   ///   a PlaylistCriterion. Just so you know.
   /// 
   public class PlaylistCriteria : IEnumerable
   {
      ///
      /// Create an object that references the given playlist index.
      ///
      public PlaylistCriteria()
      {
         matchCount = 0;
         cacheTime = DateTime.Now;
      }

      ///
      /// The number of Criterion objects in this list of criteria
      ///
      public int Count
      {
         get
         {
            return _criterionList.Count;
         }
      }

      ///
      /// \return Found criterion or null if not found
      ///  
      /// Yes, I think that's perfectly valid.
      ///
      public PlaylistCriterion Find( uint index )
      {
         if (_criterionList.Contains( index))
            return (PlaylistCriterion)_criterionList[ index ];

         return null;
      }

      public void Add( PlaylistCriterion criterion )
      {
         _Trace( "[Add] " + criterion.attribKey );
         _criterionList.Add( criterion.attribKey, criterion );
      }

      ///
      /// Remove the indicated criterion from the list.
      ///
      public void Remove( uint index )
      {
         _Trace( "[Remove] " + index );
         _criterionList.Remove( index );
      }

      ///
      /// Choose new random values for all Criteria.
      ///
      public void Randomize( Random rng )
      {
         foreach (DictionaryEntry de in this)
         {
            PlaylistCriterion crit = (PlaylistCriterion)de.Value;
            crit.Randomize( rng );
         }
      }
      
      ///
      /// Implments the IEnumerable interface
      ///
      public IEnumerator GetEnumerator()
      {
         return _criterionList.GetEnumerator();
      }

      static void _Trace( string msg )
      {
         Trace.WriteLine( msg, "PlaylistCriteria" );
      }


      /// Index of our playlist in the LocalFileDatabase
      ///
      //
      // Could also be SortedList
      private ListDictionary  _criterionList = new ListDictionary();

      ///
      ///
      /// An array containing all the primary keys found in the file_info
      /// table. This is to facilitate random searches on large databases. 
      ///
      /// This gets set to null when a query changes the database
      /// Note that I am assuming no other process modifies the database
      /// while we're using it. Which is not safe.
      ///
      /// \todo Detect database modification by other processes.
      ///
      public uint     matchCount; //  range for random numbers
      public DateTime cacheTime;
   }

   ///
   /// Value range limiting criterion (part of PlaylistCriteria)
   ///
   [Serializable]
   public class PlaylistCriterion 
      : IComparable, 
        IPlaylistCriterion
   {
      ///
      /// Unique id in the database
      ///
      uint _attribKey;
      public uint          attribKey
      {
         get
         {
            return _attribKey;
         }
         set
         {
            _attribKey = value;
         }
      }

      ///
      /// Range 0-10000.  How much the value is randomized on calls
      /// to Random.
      ///
      uint _fuzziness;
      public uint          fuzziness
      {
         get
         {
            return _fuzziness;
         }
         set
         {
            _fuzziness = value;
         }
      }

      /// 
      /// values range from 0-10000 (100.00 percent) where 100% always
      /// means we want to hear the song and 0 means we never want to.
      ///
      /// sort of :)
      ///
      public uint          value
      {
         get
         {
            return _fuzzyValue;
         }
         set
         {
            _value = value;
         }
      }

      ///
      public PlaylistCriterion( uint attribKey_,
                                uint value_,
                                uint fuzziness_ )
      {
         attribKey = attribKey_;
         fuzziness = fuzziness_;
         _fuzzyValue = value_;
         _value = value_;
      }

      ///
      /// Call to randomize the value criterion (The base value is remembered)
      ///
      public void Randomize( Random rng )
      {
         int offset = rng.Next( 0, (int)fuzziness );

         // Sometimes get more permissive, never get less.
         int newValue = (int)_value;
         newValue -= offset;

         if (newValue < 0)
            newValue = 0;
         else if (newValue > 10000)
            newValue = 10000;
         
         _fuzzyValue = (uint)newValue;
      }

      public int CompareTo( object other )
      {
         if (null == other)
            return 1;           // always greater than null

         PlaylistCriterion crit = (PlaylistCriterion)other;

         if (attribKey > crit.attribKey)
            return 1;
         else if (attribKey < crit.attribKey)
            return -1;

         return 0;
      }

      static void _Trace( string msg )
      {
         Trace.WriteLine( msg, "PlaylistCriterion" );
      }

      uint _value;              // base value
      uint _fuzzyValue;         // value after randomization
   }
}
