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
    XMLTags.h
    NVRDP

    Created by Syed Masum Billah on 10/19/15.
*/

#ifndef NVRDP_XMLTags_h
#define NVRDP_XMLTags_h

FOUNDATION_EXPORT NSString *const STRLsReq;
FOUNDATION_EXPORT NSString *const STRLsRes;
FOUNDATION_EXPORT NSString *const STRLsLongReq;
FOUNDATION_EXPORT NSString *const STRLsLongRes;
FOUNDATION_EXPORT NSString *const STRLsLongResDialog;
FOUNDATION_EXPORT NSString *const STRDelta;
FOUNDATION_EXPORT NSString *const STRDeltaSubtreeAdd;
FOUNDATION_EXPORT NSString *const STRDeltaSubtreeReplace;
FOUNDATION_EXPORT NSString *const STRDeltaSubtreeRemove;
FOUNDATION_EXPORT NSString *const STRDeltaSubtreeExpand;
FOUNDATION_EXPORT NSString *const STRDeltaSubtreeUpdate;
FOUNDATION_EXPORT NSString *const STRDeltaSubtreeMenu;
FOUNDATION_EXPORT NSString *const STRDeltaSubtreeContextMenu;
FOUNDATION_EXPORT NSString *const STRDeltaSubtreeDialog;
FOUNDATION_EXPORT NSString *const STRDeltaSubtreeKeep;
FOUNDATION_EXPORT NSString *const STRDeltaPropNameChanged;
FOUNDATION_EXPORT NSString *const STRDeltaPropValueChanged;
FOUNDATION_EXPORT NSString *const STRKeyboard;
FOUNDATION_EXPORT NSString *const STRKeyboardShortcut;
FOUNDATION_EXPORT NSString *const STRMouse;
FOUNDATION_EXPORT NSString *const STRMouseClickLeft;
FOUNDATION_EXPORT NSString *const STRMouseClickRight;
FOUNDATION_EXPORT NSString *const STRMouseScrollUp;
FOUNDATION_EXPORT NSString *const STRMouseScrollDown;
FOUNDATION_EXPORT NSString *const STRMouseMove;
FOUNDATION_EXPORT NSString *const STRMouseCaret;
FOUNDATION_EXPORT NSString *const STRAction;
FOUNDATION_EXPORT NSString *const STRActionDefault;
FOUNDATION_EXPORT NSString *const STRActionToggle;
FOUNDATION_EXPORT NSString *const STRActionSelect;
FOUNDATION_EXPORT NSString *const STRActionRename;
FOUNDATION_EXPORT NSString *const STRActionExpand;
FOUNDATION_EXPORT NSString *const STRActionCollapse;
FOUNDATION_EXPORT NSString *const STRActionClose;
FOUNDATION_EXPORT NSString *const STRActionChangeFocus;
FOUNDATION_EXPORT NSString *const STRActionChangeFocusPrecise;
FOUNDATION_EXPORT NSString *const STRActionSetText;
FOUNDATION_EXPORT NSString *const STRActionAppendText;
FOUNDATION_EXPORT NSString *const STRActionForeground;
FOUNDATION_EXPORT NSString *const STRListener;
FOUNDATION_EXPORT NSString *const STRListenerRegister;
FOUNDATION_EXPORT NSString *const STRListenerUnregister;
FOUNDATION_EXPORT NSString *const STRListenerRegisterNameChange;
FOUNDATION_EXPORT NSString *const STRListenerUnegisterNameChange;
FOUNDATION_EXPORT NSString *const STREvent;
FOUNDATION_EXPORT NSString *const STREventClosed;
FOUNDATION_EXPORT NSString *const STRVerifyPasscode;
FOUNDATION_EXPORT NSString *const STRVerifyPasscodeReq;
FOUNDATION_EXPORT NSString *const STRVerifyPasscodeRes;

FOUNDATION_EXPORT NSString *const rootTag;
FOUNDATION_EXPORT NSString *const idTag;
FOUNDATION_EXPORT NSString *const headerTag;
FOUNDATION_EXPORT NSString *const serviceCodeTag;
FOUNDATION_EXPORT NSString *const timestampTag;
FOUNDATION_EXPORT NSString *const applicationTag;

FOUNDATION_EXPORT NSString *const processIdTag;
FOUNDATION_EXPORT NSString *const nameTag;
FOUNDATION_EXPORT NSString *const valueTag;
FOUNDATION_EXPORT NSString *const roleTag;
FOUNDATION_EXPORT NSString *const typeTag;
FOUNDATION_EXPORT NSString *const leftTag;
FOUNDATION_EXPORT NSString *const topTag;
FOUNDATION_EXPORT NSString *const widthTag;
FOUNDATION_EXPORT NSString *const heightTag;
FOUNDATION_EXPORT NSString *const statesTag;
FOUNDATION_EXPORT NSString *const childCountTag;


FOUNDATION_EXPORT NSString *const targetIdTag;
FOUNDATION_EXPORT NSString *const updateTypeTag;

FOUNDATION_EXPORT NSString *const eventTag;
FOUNDATION_EXPORT NSString *const eventFGTag;
FOUNDATION_EXPORT NSString *const eventKBDTag;
FOUNDATION_EXPORT NSString *const eventMOUSETag;
FOUNDATION_EXPORT NSString *const eventFocusTag;
FOUNDATION_EXPORT NSString *const eventActionTag;
FOUNDATION_EXPORT NSString *const eventSetTextTag;
FOUNDATION_EXPORT NSString *const eventAppendTextTag;

FOUNDATION_EXPORT NSString *const updateTypeChildUpdated;
FOUNDATION_EXPORT NSString *const updateTypeChildReplaced;
FOUNDATION_EXPORT NSString *const updateTypeNameChanged;
FOUNDATION_EXPORT NSString *const updateTypeValueChanged;
FOUNDATION_EXPORT NSString *const updateTypeChildAdded;
FOUNDATION_EXPORT NSString *const updateTypeFocusChanged;
FOUNDATION_EXPORT NSString *const updateTypeDialog;
FOUNDATION_EXPORT NSString *const updateTypeNodeExpanded;


#endif
