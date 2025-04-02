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

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectBookmarkFolder : DirectObject
    {
        #region Constructors


       // Dumping attributes of<KeyVal: {'folderID': 1234567, 'description': u'', 'useGroupID': None, 'manageGroupID': None, 'adminGroupID': None,
       //'folderName': u'name', 'isPersonal': True, 'creatorID': 1234567890, 'ownerID': 1234567890, 'accessLevel': 1, 'viewGroupID': None, 'isActive': True
    

        internal DirectBookmarkFolder(DirectEve directEve, PyObject pyFolder)
            : base(directEve)
        {
            Id = (long)pyFolder.Attribute("folderID");
            Name = (string)pyFolder.Attribute("folderName");
            OwnerId = (long)pyFolder.Attribute("ownerID");
            CreatorId = (long?)pyFolder.Attribute("creatorID");
            IsPersonal = (bool)pyFolder.Attribute("isPersonal");
            IsActive = (bool)pyFolder.Attribute("isActive");
            PyObject = pyFolder;
        }

        #endregion Constructors

        #region Properties

        public PyObject PyObject { get; internal set; }

        public long? CreatorId { get; internal set; }
        public long Id { get; internal set; }
        public string Name { get; internal set; }
        public long OwnerId { get; internal set; }
        public bool IsPersonal { get; internal set; }
        public bool IsActive { get; internal set; }
        #endregion Properties

        #region Methods

        public bool Delete()
        {
            return DirectEve.ThreadedLocalSvcCall("bookmarkSvc", "DeleteBookmarkFolder", Id);
        }

        #endregion Methods
    }
}