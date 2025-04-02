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
using System;
using System.Linq;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum ScanGroup
    {
        Starbase = 0,
        Scrap = 1,
        Fighter = 2,
        Signature = 3,
        Ship = 4,
        Structure = 5,
        Drone = 6,
        Celestial = 7,
        Anomaly = 8,
        Charge = 9,
        NPC = 10,
        Orbital = 11,
        Deployable = 12,
        Sovereignty = 13,
        FilamentTrace = 14,
    }

    public class DirectSystemScanResult : DirectObject
    {
        #region Fields

        internal PyObject PyResult;

        #endregion Fields

        //'itemID': 1026895148307L,
        //'typeID': None,
        //'isIdentified': False,
        //'scanGroupID': 4,
        //'factionID': None,
        //'difficulty': None,
        //'data': (-76322780977.72708, -25978895447.96276, 430583048001.9394),
        //'certainty': 0.08137081820865469,
        //'prevCertainty': 0.08137081886993291,
        //'pos': (-76322780977.72708, -25978895447.96276, 430583048001.9394),
        //'groupID': None,
        //'strengthAttributeID': None,
        //'isPerfect': False,
        //'dungeonNameID': None,
        //'GetDistance': <bound method Result._GetDistance of <Result : HPK-944 - 4, None, None>>,
        //'id': 'HPK-943'}>

        #region Constructors

        internal DirectSystemScanResult(DirectEve directEve, PyObject pyResult)
            : base(directEve)
        {
            PyResult = pyResult;
            Id = (string)pyResult.Attribute("id");
            ScanGroupID = (int)pyResult.Attribute("scanGroupID");
            TypeID = (int)pyResult.Attribute("typeID");
            GroupID = (int)pyResult.Attribute("groupID");
            ScanGroup = (ScanGroup)ScanGroupID;
            IsPerfectResult = (bool)pyResult.Attribute("isPerfect");
            //GroupName = (string) pyResult.Attribute("groupName").ToUnicodeString();
            //TypeName = (string) pyResult.Attribute("typeName").ToUnicodeString();
            SignalStrength = (double)pyResult.Attribute("certainty");
            PreviousSignalStrength = (double)pyResult.Attribute("prevCertainty");
            Deviation = (double)pyResult.Attribute("deviation");
            var pos = pyResult.Attribute("pos");
            var data = pyResult.Attribute("data");

            Pos = new Vec3((double)pos.GetItemAt(0), (double)pos.GetItemAt(1), (double)pos.GetItemAt(2));
            // Data can also be a float
            Data = data.GetPyType() == PyType.TupleType ? new Vec3((double)data.GetItemAt(0), (double)data.GetItemAt(1), (double)data.GetItemAt(2)) : new Vec3(0, 0, 0);
            IsPointResult = (string)PyResult.Attribute("data").Attribute("__class__").Attribute("__name__") == "tuple";
            IsSphereResult = (string)PyResult.Attribute("data").Attribute("__class__").Attribute("__name__") == "float";
            MultiPointResult = (string)PyResult.Attribute("data").Attribute("__class__").Attribute("__name__") == "list";
            //IsCircleResult = !IsPointResult && !IsSpereResult;
            //if (IsPointResult)
            //{
            //    X = (double?) pyResult.Attribute("data").Attribute("x");
            //    Y = (double?) pyResult.Attribute("data").Attribute("y");
            //    Z = (double?) pyResult.Attribute("data").Attribute("z");
            //}
            //else if (IsCircleResult)
            //{
            //    X = (double?) pyResult.Attribute("data").Attribute("point").Attribute("x");
            //    Y = (double?) pyResult.Attribute("data").Attribute("point").Attribute("y");
            //    Z = (double?) pyResult.Attribute("data").Attribute("point").Attribute("z");
            //}

            // If SphereResult: X,Y,Z is probe location

            //if (X.HasValue && Y.HasValue && Z.HasValue)
            //{
            //    var myship = directEve.ActiveShip.Entity;
            //    Distance = Math.Sqrt((X.Value - myship.X) * (X.Value - myship.X) + (Y.Value - myship.Y) * (Y.Value - myship.Y) +
            //                         (Z.Value - myship.Z) * (Z.Value - myship.Z));
            //}
            GroupName = GetGroupName();
            TypeName = GetTypeName();
        }

        #endregion Constructors

        #region Properties

        public string GetTypeName()
        {
            if (ScanGroup == ScanGroup.Signature || ScanGroup == ScanGroup.Anomaly)
            {
                if (PyResult.Attribute("dungeonNameID").IsValid)
                {
                    return DirectEve.GetLocalizationMessageById(PyResult.Attribute("dungeonNameID").ToInt());
                }
            }
            //if (PyResult.Attribute("typeID").IsValid)
            //{
            //    var typeId = PyResult.Attribute("typeID").ToInt();
            //    DirectEve.Log(typeId.ToString());
            //    //return DirectEve.GetInvType(typeId).TypeName;
            //}
            return string.Empty;
        }

        public string GetGroupName()
        {
            if (ScanGroup == ScanGroup.Signature || ScanGroup == ScanGroup.Anomaly)
            {
                if (PyResult.Attribute("strengthAttributeID").IsValid)
                {
                    var i = PyResult.Attribute("strengthAttributeID").ToInt();
                    var d = DirectEve.Const["EXPLORATION_SITE_TYPES"].ToDictionary<int>();
                    if (d.ContainsKey(i))
                    {
                        return DirectEve.GetLocalizationMessageByLabel(d[i].ToUnicodeString());
                    }
                }
            }
            //if (PyResult.Attribute("groupID").IsValid)
            //{
            //    var groupId = PyResult.Attribute("groupID").ToInt();
            //    // TODO: finish (evetypes.GetGroupNameByGroup)
            //}
            return string.Empty;
        }

        public Vec3 Data { get; internal set; }

        public string TypeName { get; internal set; }
        //public double? Distance { get; internal set; }
        //public double? X { get; internal set; }
        //public double? Y { get; internal set; }
        //public double? Z { get; internal set; }
        public double Deviation { get; internal set; }

        public int GroupID { get; internal set; }
        public string Id { get; internal set; }

        public bool IsPerfectResult { get; internal set; }
        public bool IsPointResult { get; internal set; }
        public bool IsSphereResult { get; internal set; }
        public bool MultiPointResult { get; internal set; }
        public Vec3 Pos { get; internal set; }
        public double PreviousSignalStrength { get; internal set; }
        public ScanGroup ScanGroup { get; internal set; }
        public int ScanGroupID { get; internal set; }

        public string GroupName { get; internal set; }
        public double SignalStrength { get; internal set; }

        public int TypeID { get; internal set; }

        #endregion Properties

        //public bool IsCircleResult { get; internal set; }

        #region Methods


        //def ACLBookmarkScanResult(self, locationID, name, comment, resultID, folderID, expiry, subfolderID = None):

        // BOOKMARK_EXPIRY_NONE = 0
        // BOOKMARK_EXPIRY_3HOURS = 1
        // BOOKMARK_EXPIRY_2DAYS = 2

        public bool BookmarkScanResult(string name, string folderName, string comment = "")
        {
            var folder = DirectEve.BookmarkFolders.FirstOrDefault(f => f.Name == folderName);

            if (folder == null)
            {
                DirectEve.Log($"Bookmarkfolder [{folderName}] not found.");
                return false;
            }

            return DirectEve.ThreadedLocalSvcCall("bookmarkSvc", "ACLBookmarkScanResult",
                DirectEve.Session.SolarSystemId.Value, name, comment, Id, folder.Id, 0);
        }

        public bool WarpTo()
        {
            if (SignalStrength == 1)
                return DirectEve.ThreadedLocalSvcCall("menu", "WarpToScanResult", Id);
            return false;
        }

        #endregion Methods
    }
}