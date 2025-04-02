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
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectSkills : DirectObject
    {
        #region Fields

        private List<DirectInvType> _allSkills;
        private TimeSpan? _maxQueueLength;
        private List<DirectSkill> _mySkillQueue;
        private List<DirectSkill> _mySkills;

        private TimeSpan? _skillQueueLength;

        #endregion Fields

        #region Constructors

        internal DirectSkills(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     Return all skills in the game
        /// </summary>
        public List<DirectInvType> AllSkills
        {
            get
            {
                if (_allSkills == null)
                {
                    _allSkills = new List<DirectInvType>();
                    var pySkills = DirectEve.GetLocalSvc("skills").Call("GetAllSkills").ToDictionary();
                    foreach (var s in pySkills)
                    {
                        var skill = new DirectInvType(DirectEve);
                        skill.TypeId = (int)s.Value.Attribute("typeID");
                        _allSkills.Add(skill);
                    }
                }

                return _allSkills;
            }
        }

        /// <summary>
        ///     Returns if MySkills is valid
        /// </summary>
        public bool AreMySkillsReady => DirectEve.GetLocalSvc("skills", false, false).Attribute("myskills").IsValid;

        /// <summary>
        ///     Is the skill data ready?
        /// </summary>
        public bool IsReady => DirectEve.GetLocalSvc("skillqueue").IsValid && DirectEve.GetLocalSvc("skills").IsValid;

        /// <summary>
        ///     Return the skill queue length
        /// </summary>
        public TimeSpan MaxQueueLength => (TimeSpan)(_maxQueueLength ?? (_maxQueueLength =
                                                          new TimeSpan((long)DirectEve.GetLocalSvc("skillqueue").Call("GetMaxSkillQueueLimitLength"))));

        /// <summary>
        ///     Return the current skill queue
        /// </summary>
        /// // [11:48:46] [MySkillQueue]
        /// <KeyVal: {'trainingStartSP' : 2026, 'queuePosition' : 0, 'trainingTypeID' : 3454, 'trainingDestinationSP'
        ///     : 7072, 'trainingEndTime' : 131572268400000000 L, 'trainingStartTime' : 131572066560000000 L, 'trainingToLevel' : 2}>
        public List<DirectSkill> MySkillQueue
        {
            get
            {
                if (_mySkillQueue == null)
                {
                    var pySkills = DirectEve.GetLocalSvc("skillqueue").Attribute("skillQueue").ToList();

                    _mySkillQueue = new List<DirectSkill>();
                    foreach (var s in pySkills)
                    {
                        var skill = new DirectSkill(DirectEve, PySharp.PyZero);
                        skill.TypeId = s.Attribute("trainingTypeID").ToInt();
                        skill.Level = s.Attribute("trainingToLevel").ToInt();
                        skill.TrainingStartSP = s.Attribute("trainingStartSP").ToInt();
                        skill.TrainingDestinationSP = s.Attribute("trainingDestinationSP").ToInt();
                        skill.QueuePosition = s.Attribute("queuePosition").ToInt();
                        skill.TrainingEndTime = s.Attribute("trainingEndTime").ToDateTime();
                        skill.TrainingStartTime = s.Attribute("trainingStartTime").ToDateTime();
                        skill.TrainingToLevel = s.Attribute("trainingToLevel").ToInt();
                        _mySkillQueue.Add(skill);
                    }
                }

                return _mySkillQueue;
            }
        }

        /// <summary>
        ///     Return my skills
        /// </summary>
        public List<DirectSkill> MySkills
        {
            get
            {
                if (_mySkills == null)
                    _mySkills = DirectEve.GetLocalSvc("skills").Attribute("myskills").ToDictionary().Select(s => new DirectSkill(DirectEve, s.Value)).ToList();

                return _mySkills;
            }
        }

        public bool SkillInTraining => !DirectEve.GetLocalSvc("skillqueue").Call("SkillInTraining").IsNone;

        /// <summary>
        ///     Return the skill queue length
        /// </summary>
        public TimeSpan SkillQueueLength => (TimeSpan)(_skillQueueLength ?? (_skillQueueLength =
                                                            new TimeSpan((long)DirectEve.GetLocalSvc("skillqueue").Call("GetTrainingLengthOfQueue"))));

        /// <summary>
        /// Probably worth to have the skill queue wnd open while calling
        /// </summary>
        /// <returns></returns>
        public bool AbortTrain() => SkillInTraining && DirectEve.ThreadedCall(DirectEve.GetLocalSvc("skills").Attribute("AbortTrain"));

        public bool StartTrain() => !SkillInTraining && DirectEve.ThreadedCall(DirectEve.GetLocalSvc("skillqueue").Attribute("CommitTransaction"))
                                    && DirectEve.ThreadedCall(DirectEve.GetLocalSvc("skillqueue").Attribute("BeginTransaction"));

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Add a skill to the end of the queue
        /// </summary>
        /// <param name="skill"></param>
        /// <returns></returns>
        public bool AddSkillToEndOfQueue(int typeId)
        {
            if (!AreMySkillsReady)
                return false;

            if (!CanTrainSkill(typeId))
                return false;

            // Assume level 0
            var currentLevel = 0;

            // Get the skill from 'MySkills'
            var mySkill = MySkills.FirstOrDefault(s => s.TypeId == typeId);
            if (mySkill != null)
                currentLevel = mySkill.Level;

            // Assume 1 level higher then current
            var nextLevel = currentLevel + 1;

            // Check if the skill is already in the queue
            // due to the OrderByDescending on Level, this will
            // result in the highest version of this skill in the queue
            mySkill = MySkillQueue.OrderByDescending(s => s.Level).FirstOrDefault(s => s.TypeId == typeId);
            if (mySkill != null)
                nextLevel = mySkill.Level + 1;

            if (nextLevel > 5)
                return false;

            if (nextLevel > MaxCloneSkillLevel(typeId) || IsRestricted(typeId))
                return false;

            return DirectEve.ThreadedLocalSvcCall("skillqueue", "AddSkillToEnd", typeId, currentLevel, nextLevel);
        }

        /// <summary>
        ///     Add a skill to the start of the queue
        /// </summary>
        /// <param name="skill"></param>
        /// <returns></returns>
        public bool AddSkillToFrontOfQueue(int typeId)
        {
            if (!AreMySkillsReady)
                return false;

            if (!CanTrainSkill(typeId))
                return false;

            // Assume level 1
            var toLevel = 1;

            // Get the skill from 'MySkills'
            var mySkill = MySkills.FirstOrDefault(s => s.TypeId == typeId);
            if (mySkill != null)
                toLevel = mySkill.Level + 1;

            if (toLevel > 5)
                return false;

            if (toLevel > MaxCloneSkillLevel(typeId) || IsRestricted(typeId))
                return false;

            return DirectEve.ThreadedLocalSvcCall("skillqueue", "TrainSkillNow", typeId, toLevel);
        }

        public bool CanTrainSkill(int typeId)
        {
            if (!AreMySkillsReady)
                return false;

            var inv = DirectEve.GetInvType(typeId);
            if (inv == null)
                return false;

            if (GetRequiredSkillsForType(typeId).Any())
                return false;

            return true;
        }

        /// <summary>
        ///     TODO: 2023-04-04: this return an empty dict, the method and signature however is correct
        ///     Returns the requirements for the given skill. Already trained skills are included
        ///     Only call after AreMySkillsReady == true, else the result is unspecified ( if exclude == true )
        /// </summary>
        /// <returns></returns>
        public List<Tuple<int, int>> GetRequiredSkillsForType(int typeId, bool excludeMySkills = true)
        {
            var ret = new List<Tuple<int, int>>();

            if (!AreMySkillsReady)
                return ret;

            var mySkills = MySkills;
            var dict = DirectEve.GetLocalSvc("skills").Call("GetRequiredSkillsRecursive", typeId).ToDictionary<int>();
            foreach (var kv in dict)
            {
                var key = kv.Key;
                var val = kv.Value.ToInt();
                if (excludeMySkills && mySkills.Any(s => s.TypeId == key && s.Level >= val))
                    continue;

                ret.Add(new Tuple<int, int>(key, val));
            }

            return ret;
        }

        public bool IsRestricted(int typeId)
        {
            var inv = DirectEve.GetInvType(typeId);
            if (inv == null)
                return true;
            return DirectEve.GetLocalSvc("cloneGradeSvc").Call("IsRestricted", typeId).ToBool();
        }

        public int MaxCloneSkillLevel(int typeId)
        {
            var inv = DirectEve.GetInvType(typeId);
            if (inv == null)
                return 0;
            return DirectEve.GetLocalSvc("cloneGradeSvc").Call("GetMaxSkillLevel", typeId).ToInt();
        }

        /// <summary>
        ///     Refresh MySkills
        /// </summary>
        /// <returns></returns>
        public void RefreshMySkills()
        {
            if (!AreMySkillsReady)
                DirectEve.ThreadedLocalSvcCall("skills", "RefreshMySkills");
            //            var mySkills = DirectEve.GetLocalSvc("skills").Attribute("MySkills");
            //
            //            var keywords = new Dictionary<string, object>();
            //            keywords.Add("renew", 1);
            //            return DirectEve.ThreadedCallWithKeywords(mySkills, keywords);
        }

        #endregion Methods

        // This only pauses
        //}

        // Doesn't work
        ///// <summary>
        /////   Remove's a skill from the queue (note only use DirectSkill's from the MySkillQueue list!)
        ///// </summary>
        ///// <param name="skill"></param>
        ///// <returns></returns>
        //public bool RemoveSkillFromQueue(DirectSkill skill)
        //{
        //    if (skill.PyItem.IsValid)
        //        return false;

        //    DirectEve.GetLocalSvc("skillqueue").Call("RemoveSkillFromQueue", skill.TypeId, skill.Level);
        //    if (!DirectEve.GetLocalSvc("skillqueue").Attribute("cachedSkillQueue").IsValid)
        //        return false;

        //    return DirectEve.ThreadedLocalSvcCall("skillqueue", "CommitTransaction");
        ///// <summary>
        /////   Abort the current skill in training
        ///// </summary>
        ///// <returns></returns>
        //public bool AbortTraining()
        //{
        //    var godma = DirectEve.GetLocalSvc("godma");
        //    if (!godma.Attribute("skillHandler").IsValid)
        //    {
        //        DirectEve.ThreadedCall(godma.Attribute("GetSkillHandler"));
        //        return false;
        //    }

        //    return DirectEve.ThreadedCall(godma.Attribute("skillHandler").Attribute("CharStopTrainingSkill"));
        //}
    }
}