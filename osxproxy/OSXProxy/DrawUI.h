//
//  DrawUI.h
//  NVRDP
//
//  Created by Syed Masum Billah on 11/6/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "Model.h"
#import "ControlTypes.h"

#import "CustomLabel.h"
#import "CustomTextField.h"
#import "CustomOutlineView.h"
#import "CustomTableView.h"
#import "CustomLabel.h"
#import "CustomTextView.h"
#import "CustomPathControl.h"
#import "ClientHandler.h"

#define ALL -1

@interface DrawUI : NSObject < NSTableViewDataSource, NSTableViewDelegate, NSTextViewDelegate, NSControlTextEditingDelegate, NSComboBoxDataSource, NSSearchFieldDelegate > {
    NSMutableDictionary *         idToUITable;
    NSMutableDictionary *         idTable;
    NSMutableDictionary *         screenMapTable; // to handle overlapping UI
    NSString *                    process_id;
    NSRect                        localWinFrame;

    NSWindow *                    window;
    NSCharacterSet*               numeric;
    NSCondition *                 condition;
    BOOL                          treeLoaded, listLoaded;
    NSDate*                       lasOperationTime;
    
}

- (id) initWithProcessID:(NSString*) pid
              andIDTable:(NSMutableDictionary*) _idTable
             idToUITable:(NSMutableDictionary*) _idToUITable
           screenMapTable:(NSMutableDictionary*) _screenMapTable
      havingRemoteRootUI:(Model*) _rmRoot
               andWindow:(NSWindow*) win;

- (NSRect) getLocalFrameForRemoteFrame:(Model*)child;

- (void) makeVisible:(Model*) model;

- (void) hideAllViewsUnderUI:(Model*) control;

- (BOOL) hasOverlappingPeer:(Model*) control outParam:(NSRect*) overlappingRect;

- (void) addToScreenMapTable:(Model*) control;

- (Model*) getPseudoContainerOf:(Model*) child;

# pragma mark renderer methods

- (NSSearchField *) drawSearchField:(Model*)    model frame:(NSRect)frame parentView:(NSView *) parent;
- (NSTextView *)    drawEditText:(Model*)       model frame:(NSRect)frame parentView:(NSView *) parent;
- (NSTextField*)    drawText:(Model*)           model frame:(NSRect)frame parentView:(NSView*)  parent;
- (NSTextView *)    drawSimpleEditText:(Model*) model frame:(NSRect)frame parentView:(NSView *) parent;
- (NSButton * )     drawButton:(Model*)         control frame:(NSRect)frame parentView:(NSView*)parent;
- (NSView * )       drawRadioButton:(Model*)    control frame:(NSRect)frame parentView:(NSView *) parent;
- (NSButton * )     drawCheckBox:(Model*)       control frame:(NSRect)frame parentView:(NSView *) parent;
- (NSTableView *)   drawList:(Model*)           model frame:(NSRect)frame parentView:(NSView *) parent;
- (NSOutlineView *) drawTree:(Model*)           control frame:(NSRect)frame anchor:(id      )   anchor;
- (NSPopUpButton *) drawMenuButton:(Model*)     control frame:(NSRect)frame parentView:(NSView *) parent;
- (NSComboBox *)    drawComboBox:(Model*)       control frame:(NSRect)frame parentView:(NSView*)  parent;
- (NSSegmentedControl*) drawSegmentedControl:(Model*) control frame:(NSRect)frame parentView:(NSView *) parent;
- (NSPathControl*)  drawBreadCrumb:(Model*)     control frame:(NSRect)frame parentView:(NSView *) parent;
- (NSControl*)      drawToolbar:(Model*)        control frame:(NSRect)frame parentView:(NSView *) parent;
- (id)              drawProgressBar:(Model*)    control frame:(NSRect)frame parentView:(NSView *) parent;
- (void)            drawMenuBar:(Model*)           model                     parentView:(NSMenu *) remoteMenu;
- (void) drawMenu:(Model*) model anchor:(id) anchor;
- (void) drawGroup:(Model*) control frame:(NSRect)frame;




# pragma mark class methods
+ (NSInteger) getTagForControl:(Model*) model;
+ (Model*) getParentOf:(Model*) model havingRole:(NSString *) role;

#pragma mark action handlers
- (IBAction) sendAction:(id) sender;

- (void)     sendFocus:(NSView*) sender;
- (void)     sendFocusToSynchronize:(NSView*) sender;

@property (nonatomic, strong) Model * rmUiRoot;
@property (nonatomic, strong) Model* selectedMenuModel;
@property (nonatomic, strong) Model* focusedModel;

@end
