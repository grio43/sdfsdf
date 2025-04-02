extern alias SC;

using SC::SharedComponents.EVE;
using System;
using System.Collections.Generic;

namespace EVESharpCore.Framework
{
    public static class DirectFactions
    {
        #region Fields

        private static Dictionary<FactionType, int> _factionIdByType;

        private static Dictionary<int, FactionType> _factionTypeById = new Dictionary<int, FactionType>()
        {
            {500001, FactionType.Caldari_State },
            {500002, FactionType.Minmatar_Republic },
            {500003, FactionType.Amarr_Empire },
            {500004, FactionType.Gallente_Federation },
            {500006, FactionType.CONCORD_Assembly},
            {500007, FactionType.Ammatar_Mandate},
            {500008, FactionType.Khanid_Kingdom},
            {500015, FactionType.Thukker_Tribe},
            {500018, FactionType.Mordus_Legion_Command },
            {500010, FactionType.Guristas_Pirates },
            {500011, FactionType.Angel_Cartel },
            {500012, FactionType.Blood_Raiders },
            {500019, FactionType.Sanshas_Nation },
            {500020, FactionType.Serpentis },
            {500005, FactionType.Jovian_Directorate },
            {500009, FactionType.Intaki_Syndicate },
            {500013, FactionType.Interbus },
            {500014, FactionType.ORE},
            {500016, FactionType.Sisters_of_Eve },
            {500017, FactionType.Society_of_Conscious_Thought },
            {500026, FactionType.Triglavian_Collective},
            {500027, FactionType.EDENCOM},
    };

        #endregion Fields

        #region Constructors

        static DirectFactions()
        {
            _factionIdByType = new Dictionary<FactionType, int>();
            foreach (var kv in _factionTypeById)
            {
                _factionIdByType.Add(kv.Value, kv.Key);
            }
        }

        #endregion Constructors

        #region Methods

        public static int? GetFactionIdByType(FactionType type)
        {
            if (_factionIdByType.TryGetValue(type, out var id))
                return id;

            return null;
        }

        public static FactionType GetFactionTypeById(int id)
        {
            if (_factionTypeById.TryGetValue(id, out var type))
            {
                return type;
            }
            return FactionType.Unknown;
        }

        public static FactionType GetFactionTypeByName(string s)
        {
            if (Enum.TryParse<FactionType>(s.Replace("'", "").Replace(" ", "_"), out var type))
                return type;

            return FactionType.Unknown;
        }

        #endregion Methods
    }
}