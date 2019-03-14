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
    CustomOutlineView.m
    NVRDP

    Created by Syed Masum Billah on 1/30/16.
*/

#import "CustomOutlineView.h"
#import "XMLTags.h"

@implementation CustomOutlineView

@synthesize root;
@synthesize sharedConnection;
@synthesize shouldSendKeyStrokes;

-(id) initWithFrame:(NSRect)frameRect{
    self = [super initWithFrame:frameRect];
    if (self) {
        sharedConnection = [ClientHandler getConnection];
        keyPressCount = 1;
        shouldSendKeyStrokes = NO;
    }
    return self;
}

-(id) initWithFrame:(NSRect)frameRect model:(Model*) _model andContainer:(NSView*) container{
    self = [self initWithFrame:frameRect];
    if (self) {
        root = _model;
        
        // draw scrollView
        NSScrollView *scrollview = [[NSScrollView alloc] initWithFrame:frameRect] ;
        NSSize contentSize = [scrollview contentSize];
        
        [scrollview setBorderType:NSNoBorder];
        [scrollview setHasVerticalScroller:YES];
        [scrollview setHasHorizontalScroller:NO];
        [scrollview setAutoresizingMask:NSViewWidthSizable |NSViewHeightSizable];
        
        // create outlineView
        [self setFrame: NSMakeRect(0, 0, contentSize.width-2, contentSize.height)];
        [self setIdentifier:root.unique_id];
        
        //add one column
        NSTableColumn* col = [[NSTableColumn alloc] initWithIdentifier:root.name];
        [col setWidth:root.width];
        [col setIdentifier:@"TreeColumn"];
        [[col headerCell] setStringValue:root.name];
        //    [[col dataCell] setControlSize:NSMiniControlSize];
        
        //very important two lines
        [self setOutlineTableColumn:col];
        [self addTableColumn:col];
        
        // set data source and delegates
        [self setDataSource:(id<NSOutlineViewDataSource>)self];
        [self setDelegate:(id<NSOutlineViewDelegate>)self];
        
        // set action
        [self setTarget:self];
        [self setAction:@selector(sendActionForOutlineView:)];
        //[outlineView setDoubleAction:@selector(sendActionForOutlineView2:)];
        
        //add to scrollView
        [scrollview setDocumentView:self];
        [container addSubview:scrollview];
    }
    
    //now load tableview
    treeLoaded = NO;
    [self beginUpdates];
    [self reloadData];
    [self endUpdates];
    treeLoaded = YES;
    NSLog(@"[sinter] - :%.0f: tree_update", CACurrentMediaTime()*1000000000);
    return self;
}

- (void)drawRect:(NSRect)dirtyRect {
    [super drawRect:dirtyRect];
    
    // Drawing code here.
}

#pragma mark NSResponder method
-(void)keyDown:(NSEvent *)event {
    shouldForward = YES;
    [self interpretKeyEvents:[NSArray arrayWithObject:event]];
    if (shouldForward) {
        [super keyDown:event];
    }
}

//- (void) keyUp:(NSEvent *)theEvent{
//    [self interpretKeyEvents:[NSArray arrayWithObject:theEvent]];
//    keyPressCount = 1;
//}

- (void)insertNewline:(id)sender{
    [sharedConnection sendSpecialStroke:@"{ENTER}" numRepeat:keyPressCount];
}
//
//- (void)insertText:(id)insertString {
//    // don't send it
//    //[sharedConnection sendSpecialStroke:insertString numRepeat:keyPressCount];
//}

#pragma mark NSOutlineviewDelegate method
- (NSInteger) outlineView:(NSOutlineView *)outlineView numberOfChildrenOfItem:(id) item{
    //NSLog(@"at %@ for %@", @"numberOfChildrenOfItem", [item name]);
    if (!item) {
        //Model* treeView = [idTable objectForKey:outlineView.identifier];
        Model* treeView = root;
        if (treeView) {
            return  treeView.child_count;
        }
        NSLog(@"Weird error");
    } else {
        return [(Model*)item child_count];
    }
    return 0;
}

- (BOOL) outlineView:(NSOutlineView *)outlineView isItemExpandable:(id)item{
    //NSLog(@"at %@ for %@", @"isItemExpandable", [item name]);
    if (!item) return YES;
    Model* ui = (Model*)item;
    if ( !(ui.states & STATE_COLLAPSED) && !(ui.states & STATE_EXPANDED)) {
        return NO;
    }
    return YES;
}

- (id) outlineView:(NSOutlineView *)outlineView child:(NSInteger)_index ofItem:(id) item{
    //NSLog(@"at %@ for %@ %li", @"_index ofItem", [item name], (long)_index);
    if (!item) {
        //Model* treeItem = [idTable objectForKey:outlineView.identifier];
        Model* treeItem = root;
        if (treeItem && treeItem.children[_index]) {
            return treeItem.children[_index];
        }
    } else {
        if ([[item children] objectAtIndex:_index]) {
            return [[item children] objectAtIndex:_index];
        }
    }
    NSLog(@"Strange problem, a bug probably");
    return nil;
}

- (id) outlineView:(NSOutlineView *)outlineView objectValueForTableColumn:(NSTableColumn *)tableColumn byItem:(id) item{
    //NSLog(@"at %@ for %@", @"objectValueForTableColumn byItem", [item name]);
    [tableColumn setEditable:NO];
    
    Model* ui = (Model*)item;
    if (!ui) {
        return @"Not Available";
    }
    /*
     if (ui.states & STATE_EXPANDED) {
     [outlineView expandItem:item];
     }
     else if (ui.states & STATE_COLLAPSED) {
     [outlineView collapseItem:item];
     }
     else{//leaf node
     // do nothing
     }*/
    
    /*
     if (ui.states & STATE_SELECTED) {
     NSInteger itemRow = [outlineView rowForItem:ui];
     [outlineView selectRowIndexes:[NSIndexSet indexSetWithIndex:itemRow] byExtendingSelection:YES];
     }
     */
    //[self printStatesOf:(RemoteProcessUI*)item];
    return [((Model*)item).name copy];
}

- (BOOL) outlineView:(NSOutlineView *)outlineView shouldExpandItem:(id)item{
    if (!treeLoaded) {
        return YES;
    }
    //NSLog(@"at %@ for %@", @"shouldExpandItem", [item name]);
    Model *ui = (Model*) item; //[outlineView itemAtRow:[outlineView selectedRow]];
    if (ui) {
        if(ui.states & STATE_COLLAPSED){
            ui.states ^= STATE_COLLAPSED;
            //send expand action
            //NSLog(@"expand %@", ui.name);
        }
        ui.states |= STATE_EXPANDED;
        [sharedConnection sendActionMsg:nil targetId:ui.unique_id actionType:STRActionExpand data:nil];
    }
    return YES;
}

- (BOOL) outlineView:(NSOutlineView *)outlineView shouldCollapseItem:(id)item{
    if (!treeLoaded) {
        return YES;
    }
    //NSLog(@"at %@ for %@", @"shouldCollapseItem", [item name]);
    //RemoteProcessUI *ui =  [outlineView itemAtRow:[outlineView selectedRow]];
    Model *ui = (Model*) item;
    if (ui) {
        if(ui.states & STATE_EXPANDED){
            ui.states ^= STATE_EXPANDED;
            //send collapse command
            //NSLog(@"collapse %@", ui.name);
        }
        ui.states |= STATE_COLLAPSED;
        [sharedConnection sendActionMsg:nil targetId:ui.unique_id actionType:STRActionCollpase data:nil];
    }
    return YES;
}

/*
 - (BOOL) outlineView:(NSOutlineView *)outlineView shouldSelectItem:(id)item {
 NSLog(@"at %@ for %@", @"shouldSelectItem", [item name]);
 if (!item) {
 return YES;
 }
 Model *ui = (Model*) item;
 if ((ui.top == 0 && ui.left == 0 && ui.width == 0 && ui.height == 0)
 && (!(ui.states & STATE_INVISIBLE) || !(ui.states & STATE_OFFSCREEN))) {
 return NO;
 }
 return YES;
 }
 */

- (BOOL) selectionShouldChangeInOutlineView:(NSOutlineView *)outlineView{
    Model *ui =  [outlineView itemAtRow:[outlineView selectedRow]];
    if (ui && (ui.states & STATE_SELECTED)) {
        ui.states ^= STATE_SELECTED;
    }
    return YES;
}

- (void) outlineViewSelectionDidChange:(NSNotification *) notification{
    if (!treeLoaded) {
        return;
    }
    
    NSOutlineView *tree = (NSOutlineView *) [notification object];
    Model *ui =  [tree itemAtRow:[tree selectedRow]];
    if(ui){
        ui.states |= STATE_SELECTED;
        //NSLog(@"selection changed %@", ui.name);
        [sharedConnection sendActionMsg:nil targetId:ui.unique_id actionType:STRActionChangeFocus data:nil];
        
    }
}

- (BOOL)outlineView:(NSOutlineView *)outlineView shouldEditTableColumn:(nullable NSTableColumn *)tableColumn item:(id)item{
    return NO;
}

- (NSCell *) outlineView:(NSOutlineView *)outlineView dataCellForTableColumn:(NSTableColumn *)tableColumn item:(id)item {
    NSTextFieldCell *cell = [[NSTextFieldCell alloc] init] ;
    [cell setEditable:NO];
    return cell;
}

#pragma mark action handler

- (IBAction) sendActionForOutlineView:(id) sender {
    NSOutlineView* outineView  = (NSOutlineView * )sender;
    NSInteger clicked_row = [outineView clickedRow];
    if (clicked_row >= 0 && clicked_row < [outineView numberOfRows]) {
        Model *ui =  [outineView itemAtRow:clicked_row];
        if (ui) { // KeystorkesAt:process_id strokes:@"{ENTER}"]
            [sharedConnection sendActionMsg:nil targetId:ui.unique_id actionType:STRActionChangeFocus data:nil];
        }
        [outineView selectRowIndexes:[NSIndexSet indexSetWithIndex:clicked_row] byExtendingSelection:NO];
    }
}

@end
