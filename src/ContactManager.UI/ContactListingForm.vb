﻿Imports System.ComponentModel
Imports System.IO
Imports System.Security.Cryptography
Imports Newtonsoft.Json
Imports ContactManager.Core
Imports ContactManager.Data

Public Class ContactListingForm
    Public Const AppName = "Contact Manager"
    Private contacts As New List(Of Contact)()

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.Icon = My.Resources.Icon
    End Sub

    Private Async Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            Await SQLiteHelper.SetupConnectionAsync()
            Await GetContactsAsync()
        Catch ex As Exception
            MessageBox.Show(ex.ToString(), AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Sub ButtonAdd_Click(sender As Object, e As EventArgs) Handles ButtonAdd.Click
        Using addContactForm As New ContactEditorForm()
            If addContactForm.ShowDialog() = DialogResult.OK Then
                Await GetContactsAsync()
            End If
        End Using
    End Sub

    Private Async Function GetContactsAsync() As Task
        Try
            Using contactRepo As New ContactRepository()
                If CheckShowActive.Checked = True Then
                    contacts = Await contactRepo.GetContactsAsync(filterActive:=True)
                Else
                    contacts = Await contactRepo.GetContactsAsync()
                End If
                Dim search = TextBoxSearch.Text.Trim().ToLower()
                If String.IsNullOrWhiteSpace(search) Then
                    grdContacts.DataSource = contacts
                Else
                    grdContacts.DataSource = contacts.Where(Function(x) x.Name.ToLower().Contains(search) Or x.Email.Contains(search) Or x.Phone.Contains(search)).ToList()
                End If
                grdContacts.Columns("Id").Visible = False
            End Using
        Catch ex As Exception
            MessageBox.Show(ex.ToString(), AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Private Async Sub ButtonReset_Click(sender As Object, e As EventArgs) Handles ButtonReset.Click
        If MessageBox.Show("This will Reset Database. Are you sure?", AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Await SQLiteHelper.ResetDatabaseAsync()
            Await GetContactsAsync()
        End If
    End Sub

    Private Async Sub ButtonEdit_Click(sender As Object, e As EventArgs) Handles ButtonEdit.Click
        Dim selectedContact = GetSelectedContact()

        If selectedContact Is Nothing Then
            MessageBox.Show($"Select a contact to Edit.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return
        End If

        Using contactEditorForm As New ContactEditorForm(selectedContact)
            If contactEditorForm.ShowDialog() = DialogResult.OK Then
                Await GetContactsAsync()
            End If
        End Using
    End Sub

    Private Function GetSelectedContact() As Contact
        If grdContacts.SelectedCells.Count > 0 Then
            Dim rowIndex As Integer = grdContacts.SelectedCells(0).RowIndex
            If rowIndex >= 0 And rowIndex < grdContacts.Rows.Count Then
                Dim selectedContact As Contact = CType(grdContacts.Rows(rowIndex).DataBoundItem, Contact)
                Return selectedContact
            End If
        End If

        Return Nothing
    End Function

    Private Async Sub ButtonDelete_Click(sender As Object, e As EventArgs) Handles ButtonDelete.Click
        Dim selectedContact = GetSelectedContact()

        If selectedContact Is Nothing Then
            MessageBox.Show($"Select a contact to DELETE.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return
        End If

        If MessageBox.Show("This will DELETE the selected contact. Are you sure?", AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Using contactRepo As New ContactRepository()
                Await contactRepo.DeleteContactAsync(selectedContact)
            End Using
            Await GetContactsAsync()
        End If
    End Sub

    Private Async Sub CheckShowActive_CheckedChanged(sender As Object, e As EventArgs) Handles CheckShowActive.CheckedChanged
        Await GetContactsAsync()
    End Sub

    Private Sub ButtonExport_Click(sender As Object, e As EventArgs) Handles ButtonExport.Click

        Dim saveFileDialog As New SaveFileDialog With {
            .DefaultExt = "json",
            .Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
        }

        If saveFileDialog.ShowDialog() = DialogResult.OK Then
            Dim filePath As String = saveFileDialog.FileName

            Dim jsonString As String = JsonConvert.SerializeObject(contacts)

            File.WriteAllText(filePath, jsonString)

            MessageBox.Show("Export file saved successfully!", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Async Sub ButtonImport_Click(sender As Object, e As EventArgs) Handles ButtonImport.Click
        Dim openFileDialog As New OpenFileDialog With {
            .DefaultExt = "json",
            .Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
        }

        If openFileDialog.ShowDialog() = DialogResult.OK Then
            Dim filePath As String = openFileDialog.FileName

            Try
                Dim importedContacts = JsonConvert.DeserializeObject(Of List(Of Contact))(File.ReadAllText(filePath))
                Using contactRepo As New ContactRepository()
                    Dim result = Await contactRepo.BatchInsertAsync(importedContacts)
                    If result = False Then
                        Throw New Exception("Error batch saving")
                    End If
                End Using
                Await GetContactsAsync()
                MessageBox.Show("Import file saved successfully!", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show(ex.ToString(), AppName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    Private Async Sub ButtonSearch_Click(sender As Object, e As EventArgs) Handles ButtonSearch.Click
        Await GetContactsAsync()
    End Sub
End Class
