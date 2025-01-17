﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagementApp
{
    public class CTypeConverters
    {
        public class CProjectStatusConverter : StringConverter
        {
			public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
			{
				//true means show a combobox
				return true;
			}
			//----------------------------------------------------------------------------
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
			{
				//true will limit to list. false will show the list, but allow free-form entry
				return true;
			}
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                string[] labels = CJsonDatabase.Instance.GetProjectStatusLabels();
                return new StandardValuesCollection(labels);
            }
        }
        public class CProjectSourceControlConverter : StringConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                //true means show a combobox
                return true;
            }
            //----------------------------------------------------------------------------
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                //true will limit to list. false will show the list, but allow free-form entry
                return true;
            }
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return new StandardValuesCollection(CDefines.PROJ_SRCCTRL_LABELS);
            }
        }
        public class CProjectTypeConverter : StringConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                //true means show a combobox
                return true;
            }
            //----------------------------------------------------------------------------
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                //true will limit to list. false will show the list, but allow free-form entry
                return true;
            }
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                string[] labels = CJsonDatabase.Instance.GetProjectTypeLabels();
                return new StandardValuesCollection(labels);
            }
        }
    }
}
