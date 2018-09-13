﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using XTMF.Gui.Controllers;

namespace XTMF.Gui.UserControls
{
    public partial class ModelSystemDisplay
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled == false)
            {
                if (EditorController.IsShiftDown() && EditorController.IsControlDown())
                {
                    switch (e.Key)
                    {
                        case Key.F:
                            if (QuickParameterDisplay2.IsEnabled)
                            {
                                QuickParameterDialogHost.IsOpen = true;
                            }
                            else if (ModuleParameterDisplay.IsEnabled)
                            {
                                ModuleParameterDialogHost.IsOpen = true;
                            }

                            break;
                    }
                }

                if (EditorController.IsControlDown())
                {
                    switch (e.Key)
                    {
                        case Key.M:
                            if (EditorController.IsAltDown())
                            {
                                //SetMetaModuleStateForSelected(false);
                            }
                            else if (EditorController.IsShiftDown())
                            {
                                //SetMetaModuleStateForSelected(true);
                            }
                            else
                            {
                                SelectReplacement();
                            }

                            e.Handled = true;
                            break;
                        case Key.R:
                            //ParameterTabControl.SelectedIndex = 2;
                            //Mo.Focus();
                            // Keyboard.Focus(ParameterFilterBox);
                            e.Handled = true;
                            break;
                        case Key.P:
                            //ParameterTabControl.SelectedIndex = 1;
                            //ModuleParameterTab.Focus();
                            ModuleParameterDisplay.Focus();
                            Keyboard.Focus(ParameterFilterBox);
                            e.Handled = true;
                            break;
                        case Key.E:
                            FilterBox.Focus();
                            e.Handled = true;
                            break;
                        case Key.W:
                            Close();
                            e.Handled = true;
                            break;
                        case Key.L:
                            ShowLinkedParameterDialog(true);
                            e.Handled = true;
                            break;
                        case Key.N:
                            CopyParameterName();
                            e.Handled = true;
                            break;
                        case Key.O:
                            OpenParameterFileLocation(false, false);
                            e.Handled = true;
                            break;
                        case Key.F:
                            SelectFileForCurrentParameter();
                            e.Handled = true;
                            break;
                        case Key.D:
                            if (ModuleParameterDisplay.IsKeyboardFocusWithin)
                            {
                                SelectDirectoryForCurrentParameter();
                            }

                            //TODO
                            //if (ModuleDisplay.IsKeyboardFocusWithin)
                            //{
                            //    ToggleDisableModule();
                            //}

                            e.Handled = true;
                            break;
                        case Key.Z:
                            Undo();
                            e.Handled = true;
                            break;
                        case Key.Y:
                            Redo();
                            e.Handled = true;
                            break;
                        case Key.S:
                            SaveRequested(false);
                            e.Handled = true;
                            break;
                        case Key.C:
                            CopyCurrentModule();
                            e.Handled = true;
                            break;
                        case Key.V:
                            PasteCurrentModule();
                            e.Handled = true;
                            break;
                        case Key.Q:

                            if (EditorController.IsShiftDown())
                            {
                                ToggleQuickParameterDisplay();
                                e.Handled = true;
                            }
                            else
                            {
                                ToggleQuickParameterDisplaySearch();

                                e.Handled = true;
                                break;
                            }


                            break;
                    }
                }
                else
                {
                    switch (e.Key)
                    {
                        case Key.F2:
                            if (EditorController.IsShiftDown())
                            {
                                RenameDescription();
                            }
                            else
                            {
                                RenameParameter();
                            }

                            break;
                        case Key.F1:
                            ShowDocumentation();
                            e.Handled = true;
                            break;
                        case Key.Delete:
                            if ((ModelSystemDisplayContent.Content as UserControl).IsKeyboardFocusWithin)
                            {
                                RemoveSelectedModules();
                                e.Handled = true;
                            }

                            break;
                        case Key.F5:
                            e.Handled = true;
                            SaveCurrentlySelectedParameters();
                            ExecuteRun();

                            break;
                        case Key.Escape:
                            FilterBox.Box.Text = string.Empty;
                            break;
                    }
                }
            }
        }
    }
}