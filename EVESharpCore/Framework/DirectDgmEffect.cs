extern alias SC;

using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    public class DirectDgmEffect : DirectObject
    {
        #region Fields

        #endregion Fields

        #region Constructors

        internal DirectDgmEffect(DirectEve directEve, PyObject py) : base(directEve)
        {
            EffectName = py.Attribute("effectName").ToUnicodeString();
            DisplayName = py.Attribute("displayName").ToUnicodeString();
            EffectID = py.Attribute("effectID").ToInt();
            Guid = py.Attribute("guid").ToUnicodeString();
        }

        #endregion Constructors

        #region Properties

        public string DisplayName { get; private set; }
        public int EffectID { get; private set; }
        public string EffectName { get; private set; }
        public string Guid { get; private set; }

        #endregion Properties
    }

    //[<Instance of class cfg.DgmEffect>
    //effectID:               1281
    //effectName:             structuralAnalysisEffect
    //effectCategory:         0
    //preExpression:          5408
    //postExpression:         5409
    //description:            Automatically generated effect
    //guid:
    //isOffensive:            False
    //isAssistance:           False
    //durationAttributeID:    None
    //trackingSpeedAttributeID:None
    //dischargeAttributeID:   None
    //rangeAttributeID:       None
    //falloffAttributeID:     None
    //disallowAutoRepeat:     False
    //published:              False
    //displayName:
    //isWarpSafe:             False
    //rangeChance:            False
    //electronicChance:       False
    //propulsionChance:       False
    //distribution:           None
    //sfxName:                None
    //npcUsageChanceAttributeID:None
    //npcActivationChanceAttributeID:None
    //fittingUsageChanceAttributeID:None
    //iconID:                 0
    //displayNameID:          None
    //descriptionID:          None
    //modifierInfo:           None
    //resistanceID:           None
    //dataID:                 94710827
    //, <Instance of class cfg.DgmEffect>
    //effectID:               1395
    //effectName:             shieldBoostAmplifierPassive
    //effectCategory:         0
    //preExpression:          3171
    //postExpression:         3172
    //description:            Automatically generated effect
    //guid:
    //isOffensive:            False
    //isAssistance:           False
    //durationAttributeID:    None
    //trackingSpeedAttributeID:None
    //dischargeAttributeID:   None
    //rangeAttributeID:       None
    //falloffAttributeID:     None
    //disallowAutoRepeat:     False
    //published:              False
    //displayName:
    //isWarpSafe:             False
    //rangeChance:            False
    //electronicChance:       False
    //propulsionChance:       False
    //distribution:           None
    //sfxName:                None
    //npcUsageChanceAttributeID:None
    //npcActivationChanceAttributeID:None
    //fittingUsageChanceAttributeID:None
    //iconID:                 0
    //displayNameID:          None
    //descriptionID:          None
    //modifierInfo:           None
    //resistanceID:           None
    //dataID:                 94710847
    //]


}