// $Id$
// Abstract interface to playlist criteria

namespace tam
{
   public interface IPlaylistCriterion
   {
      uint attribKey { get; }
      uint fuzziness { get; }
      uint value { get; }
   }
}
