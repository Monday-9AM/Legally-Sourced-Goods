﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ClassLibrary;

public partial class StaffDataEntry : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void btnOK_Click(object sender, EventArgs e)
    {
        // Creates a new instance of the staff class.
        clsStaff StaffMember = new clsStaff();

        // Capture the data.
        string StaffPassword = txtStaffPassword.Text;
        string RecordCreated = txtRecordCreated.Text;
        string FirstName = txtFirstName.Text;
        string FamilyName = txtFamilyName.Text;

        // Variable to store any error messages.
        string Error = "";

        Error = StaffMember.Valid(StaffPassword, RecordCreated, FirstName, FamilyName);
        if (Error == "")
        {
            // Captures the data.
            StaffMember.StaffPassword = StaffPassword;
            StaffMember.IsManager = chkIsManager.Checked;
            StaffMember.RecordCreated = Convert.ToDateTime(RecordCreated);
            StaffMember.FirstName = FirstName;
            StaffMember.FamilyName = FamilyName;

            // Create a new instance of the staff collection.
            clsStaffCollection StaffMembers = new clsStaffCollection();

            // Set the ThisStaff property.
            StaffMembers.ThisStaff = StaffMember;

            // Add the new record.
            StaffMembers.Add();

            // Redirect to the StaffList page.
            Response.Redirect("StaffList.aspx");
        }
        else
        {
            lblError.Text = Error;
        }

    }

    protected void btnFind_Click(object sender, EventArgs e)
    {
        // Creates a new instance of the staff class.
        clsStaff StaffMember = new clsStaff();

        // Variable to sote the primnary key.
        Int32 StaffID;

        // Variable to store the result of the find operation.
        Boolean Found = false;

        // Gets the primary key value entered by the user.
        StaffID = Convert.ToInt32(txtStaffID.Text);

        // Finds the record using the StaffID (PK).
        Found = StaffMember.Find(StaffID);

        // If the records is found.
        if (Found == true)
        {
            txtStaffID.Text = StaffMember.StaffID.ToString();
            txtStaffPassword.Text = StaffMember.StaffPassword;
            txtRecordCreated.Text = StaffMember.RecordCreated.ToString();
            txtFirstName.Text = StaffMember.FirstName;
            txtFamilyName.Text = StaffMember.FamilyName;
        }
    }
}


