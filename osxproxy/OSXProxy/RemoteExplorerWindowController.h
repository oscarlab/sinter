//
//  RemoteExplorerWindowController.h
//  NVRDP
//
//  Created by Syed Masum Billah on 8/1/14.
//  Copyright (c) 2014 Stony Brook University. All rights reserved.
//

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
