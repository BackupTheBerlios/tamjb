/// \file
/// $Id$

namespace tam
{
   ///
   /// An object that handles scanning recursively through 
   /// subdirectories looking for mp3 files, in a polling
   /// interface suitable for non-threaded applications. :)
   ///
   protected class FileScanner
   {
      public FileScanner()
      {
      }

      public void AddDir( string dir )
      {
         _dirs.Add( dir );
      }
      
      public void CheckOneDirectory();
      {
         if (_dirs.Length == 0) // nothing to do
            return;

         if (!_isScanning)      // start now?
         {
            _currentDir = 0;
            _isScanning = true;
         }

         

         // If we're done with this dir, go on to the next one
         if (_currentDir > _dirs.Length)
            _currentDir = 0;

         string thisDir = _dirs[currentDir];
         
      }

      ArrayList _dirs = new ArrayList();
      int       _currentDir = 0;
      bool      _isScanning = false;
   }
}
