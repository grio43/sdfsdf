// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using SC::SharedComponents.Py;
using System;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    /// <summary>
    ///     Skill in the game
    /// </summary>
    public class DirectSkill : DirectItem
    {
        #region Fields

        private int? _level;
        private int? _skillPoints;
        private int? _skillRank;

        #endregion Fields

        #region Constructors

        internal DirectSkill(DirectEve directEve, PyObject pySkill) : base(directEve)
        {
            PyItem = pySkill;
        }

        #endregion Constructors

        //        internal PyObject PyGodmaItem
        //        {
        //            get
        //            {
        //                if (!PyItem.IsValid)
        //                    return PySharp.PyZero;
        //
        //                return _pyGodmaItem ?? (_pyGodmaItem = DirectEve.GetLocalSvc("godma").Call("GetItem", ItemId));
        //            }
        //        }

        #region Properties

        /// <summary>
        ///     Are we currently training this skill?
        /// </summary>
        public bool InTraining => DirectEve.Skills.MySkillQueue.Any(k => k.TypeId == TypeId);

        public bool IsRestricted => DirectEve.Skills.IsRestricted(TypeId);

        /// <summary>
        ///     Level of skill
        /// </summary>
        public int Level
        {
            get => (int)(_level ?? (_level = (int)PyItem.Attribute("trainedSkillLevel")));
            set => _level = value;
        }

        public int MaxCloneSkillLevel => DirectEve.Skills.MaxCloneSkillLevel(TypeId);

        public int QueuePosition { get; set; }

        /// <summary>
        ///     Number of points in this skill
        /// </summary>
        public int SkillPoints => (int)(_skillPoints ?? (_skillPoints = (int)PyItem.Attribute("trainedSkillPoints")));

        /// <summary>
        ///     Time multiplier to indicate relative training time
        /// </summary>
        public int SkillRank => (int)(_skillRank ?? (_skillRank = (int)PyItem.Attribute("skillRank")));

        // queue

        public int TrainingDestinationSP { get; set; }
        public DateTime TrainingEndTime { get; set; }
        public int TrainingStartSP { get; set; }
        public DateTime TrainingStartTime { get; set; }
        public int TrainingToLevel { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Enqueue this skill at the end of the queue
        /// </summary>
        /// <returns></returns>
        public bool AddToEndOfQueue()
        {
            if (!PyItem.IsValid)
                return false;

            return DirectEve.Skills.AddSkillToEndOfQueue(TypeId);
        }

        /// <summary>
        ///     Train this skill right now (add it at the start of the queue)
        /// </summary>
        /// <returns></returns>
        public bool AddToFrontOfQueue()
        {
            if (!PyItem.IsValid)
                return false;

            return DirectEve.Skills.AddSkillToFrontOfQueue(TypeId);
        }

        #endregion Methods

        // TODO: This doesnt work :(
        ///// <summary>
        /////   Remove this skill from the queue (only use on skills from the MySkillQueue list)
        ///// </summary>
        ///// <returns></returns>
        //public bool RemoveFromQueue()
        //{
        //    return DirectEve.Skills.RemoveSkillFromQueue(this);
        //}
    }
}