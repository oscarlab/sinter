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
    RemoteExplorerWindowController.h
    NVRDP

    Created by Syed Masum Billah on 8/1/14.
*/

#import <Cocoa/Cocoa.h>

@interface RemoteExplorerWindowController : NSWindowController //<NSOutlineViewDelegate, NSSplitViewDelegate>{

@property (weak) IBOutlet NSComboBox *txtName;
@property (weak) IBOutlet NSComboBox *txtType;

@property (weak) IBOutlet NSComboBox *comboAdress;
    
@property (weak) IBOutlet NSTextField *txtSearch;
@property (weak) IBOutlet NSPopUpButton *popOrganize;
    

- (IBAction)save:(id)sender;
- (IBAction)cancel:(id)sender;

- (IBAction)goBack:(id)sender;
- (IBAction)goForward:(id)sender;
- (IBAction)help:(id)sender;
- (IBAction)newFolder:(id)sender;

@end
