/* Copyright (C) 2014--2018 Stony Brook University
   Copyright (C) 2016--2018 The University of North Carolina at Chapel Hill

   This file is part of the Sinter Remote Desktop System.

   Sinter is dual-licensed, available under a commercial license or
   for free subject to the LGPL.  

   Sinter is free software: you can redistribute it and/or modify it
   under the terms of the GNU Lesser General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.  Sinter is distributed in the
   hope that it will be useful, but WITHOUT ANY WARRANTY; without even
   the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
   PURPOSE.  See the GNU Lesser General Public License for more details.  You
   should have received a copy of the GNU Lesser General Public License along
   with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

/*
    XMLTags.m
    NVRDP

    Created by Syed Masum Billah on 10/19/15.
*/

#import <Foundation/Foundation.h>

NSString *const STRLsReq = @"ls_req";
NSString *const STRLsRes = @"ls_res";
NSString *const STRLsLongReq = @"ls_l_req";
NSString *const STRLsLongRes = @"ls_l_res";
NSString *const STRLsLongResDialog = @"ls_l_res_dialog";
NSString *const STRDelta = @"delta";
NSString *const STRDeltaSubtreeAdd = @"delta_subtree_add";
NSString *const STRDeltaSubtreeReplace = @"delta_subtree_replace";
NSString *const STRDeltaSubtreeRemove = @"delta_subtree_remove";
NSString *const STRDeltaSubtreeExpand = @"delta_subtree_expand";
NSString *const STRDeltaSubtreeUpdate = @"delta_subtree_update";
NSString *const STRDeltaSubtreeMenu = @"delta_subtree_menu";
NSString *const STRDeltaSubtreeContextMenu = @"delta_subtree_context_menu";
NSString *const STRDeltaSubtreeDialog = @"delta_subtree_dialog";
NSString *const STRDeltaSubtreeKeep = @"delta_subtree_keep";
NSString *const STRDeltaPropNameChanged = @"delta_prop_change_name";
NSString *const STRDeltaPropValueChanged = @"delta_prop_change_value";
NSString *const STRKeyboard = @"kbd";
NSString *const STRKeyboardShortcut = @"kbd_shortcut";
NSString *const STRMouse = @"mouse";
NSString *const STRMouseClickLeft = @"mouse_click_left";
NSString *const STRMouseClickRight = @"mouse_click_right";
NSString *const STRMouseScrollUp = @"mouse_scroll_up";
NSString *const STRMouseScrollDown = @"mouse_scroll_down";
NSString *const STRMouseMove = @"mouse_move";
NSString *const STRMouseCaret = @"mouse_caret";
NSString *const STRAction = @"action";
NSString *const STRActionDefault = @"action_default";
NSString *const STRActionToggle = @"action_toggle";
NSString *const STRActionSelect = @"action_select";
NSString *const STRActionRename = @"action_rename";
NSString *const STRActionExpand = @"action_expand";
NSString *const STRActionCollapse = @"action_collapse";
NSString *const STRActionClose = @"action_close";
NSString *const STRActionChangeFocus = @"action_change_focus";
NSString *const STRActionChangeFocusPrecise = @"action_change_focus_precise";
NSString *const STRActionSetText = @"action_set_text";
NSString *const STRActionAppendText = @"action_append_text";
NSString *const STRActionForeground = @"action_foreground";
NSString *const STRListener = @"listener";
NSString *const STRListenerRegister = @"listener_register";
NSString *const STRListenerUnregister = @"listener_unregister";
NSString *const STRListenerRegisterNameChange = @"listener_register_name_change";
NSString *const STRListenerUnegisterNameChange = @"listener_unregister_name_change";
NSString *const STREvent = @"event";
NSString *const STREventClosed = @"event_closed";
NSString *const STRVerifyPasscode = @"verify_passcode";
NSString *const STRVerifyPasscodeReq = @"verify_passcode_req";
NSString *const STRVerifyPasscodeRes = @"verify_passcode_res";

NSString *const rootTag              = @"sinter";
NSString *const idTag                = @"id";
NSString *const headerTag            = @"header";
NSString *const timestampTag         = @"timestamp";
NSString *const serviceCodeTag       = @"service_code";
NSString *const applicationTag       = @"application";
NSString *const processIdTag         = @"process_id";
NSString *const nameTag              = @"name";
NSString *const valueTag             = @"value";
NSString *const roleTag              = @"role";
NSString *const typeTag              = @"type";
NSString *const leftTag              = @"left";
NSString *const topTag               = @"top";
NSString *const widthTag             = @"width";
NSString *const heightTag            = @"height";
NSString *const statesTag            = @"states";
NSString *const childCountTag        = @"child_count";
NSString *const targetIdTag          = @"target_id";
NSString *const updateTypeTag        = @"update_type";

#pragma mark update type
NSString *const updateTypeChildUpdated     = @"child_updated";
NSString *const updateTypeChildReplaced    = @"child_replaced";
NSString *const updateTypeNameChanged      = @"name_changed";
NSString *const updateTypeValueChanged     = @"value_changed";
NSString *const updateTypeChildAdded       = @"child_added";
NSString *const updateTypeFocusChanged     = @"focus_changed";
NSString *const updateTypeNodeExpanded     = @"node_expanded";
NSString *const updateTypeDialog           = @"dialog";

