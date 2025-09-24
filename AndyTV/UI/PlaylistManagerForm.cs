﻿using System.ComponentModel;
using AndyTV.Helpers;
using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.UI;

public sealed class PlaylistManagerForm : Form
{
    public bool Saved { get; private set; } = false;

    private bool HasChanges => UtilHelper.GenerateSnapshot(_data) != _snapshot;

    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
    };

    private readonly Button _btnAdd;
    private readonly Button _btnDelete;
    private readonly Button _btnSave;

    private readonly BindingList<Playlist> _data = [];
    private string _snapshot = "";

    public PlaylistManagerForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "Playlists";
        StartPosition = FormStartPosition.CenterParent;
        Width = 720;
        Height = 500;

        _btnAdd = UIHelper.CreateButton("Add", OnAdd);
        _btnDelete = UIHelper.CreateButton("Delete", OnDelete);
        _btnSave = UIHelper.CreateButton("Save", (_, __) => Save());

        _btnAdd.Margin = new Padding(0, 0, 8, 0);
        _btnDelete.Margin = new Padding(0, 0, 8, 0);
        _btnSave.Margin = new Padding(0);

        _data = new BindingList<Playlist>(PlaylistChannelService.Load());
        _snapshot = UtilHelper.GenerateSnapshot(_data);

        _grid.DataSource = _data;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.AllowUserToResizeRows = false; // 👈 disable row resizing
        _grid.RowTemplate.Height = 28; // 👈 enforce fixed row height
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;

        _grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Playlist.Name),
                HeaderText = nameof(Playlist.Name),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 35,
            }
        );
        _grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Playlist.Url),
                HeaderText = nameof(Playlist.Url),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 55,
            }
        );
        _grid.Columns.Add(
            new DataGridViewCheckBoxColumn
            {
                DataPropertyName = nameof(Playlist.ShowInMenu),
                HeaderText = "Show in Menu",
                Width = 120,
            }
        );

        // ---- Bottom bar ----
        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(8),
            Margin = new Padding(0),
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var left = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        left.Controls.AddRange([_btnAdd, _btnDelete]);

        var right = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        right.Controls.Add(_btnSave);

        bottom.Controls.Add(left, 0, 0);
        bottom.Controls.Add(new Panel { Dock = DockStyle.Fill, Height = 0 }, 1, 0);
        bottom.Controls.Add(right, 2, 0);

        Controls.Add(_grid);
        Controls.Add(bottom);

        FormClosing += OnFormClosing;
    }

    private void OnAdd(object sender, EventArgs e)
    {
        _data.Add(
            new Playlist
            {
                Name = "New Playlist",
                Url = "https://",
                ShowInMenu = true,
            }
        );

        var idx = _data.Count - 1;
        _grid.CurrentCell = _grid.Rows[idx].Cells[0];
        _grid.BeginEdit(true);
    }

    private void OnDelete(object sender, EventArgs e)
    {
        if (_grid.CurrentRow?.DataBoundItem is not Playlist p)
            return;

        var ok = MessageBox.Show(
            this,
            $"Delete playlist '{p.Name}'?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (ok == DialogResult.Yes)
        {
            _data.Remove(p);
        }
    }

    private void OnFormClosing(object sender, FormClosingEventArgs e)
    {
        _grid.EndEdit();
        Validate();

        if (!HasChanges)
            return;

        var r = MessageBox.Show(
            this,
            "Save changes?",
            "Playlists",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question
        );

        if (r == DialogResult.Cancel)
        {
            e.Cancel = true;
            return;
        }

        if (r == DialogResult.Yes)
        {
            Save();
        }
    }

    private void Save()
    {
        _grid.EndEdit();
        Validate();

        if (!_data.All(x => UtilHelper.IsValidUrl(x.Url)))
        {
            MessageBox.Show(
                this,
                "Valid URL required for save.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            Saved = false;
            return;
        }

        if (!HasChanges)
        {
            Saved = false;
            return;
        }

        PlaylistChannelService.Save([.. _data]);
        _snapshot = UtilHelper.GenerateSnapshot(_data);
        Saved = true;

        MessageBox.Show(
            this,
            "Save successful.",
            "Playlists",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
}