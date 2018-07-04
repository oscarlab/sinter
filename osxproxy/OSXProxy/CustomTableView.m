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
    CustomTableView.m
    NVRDP

    Created by Syed Masum Billah on 1/30/16.
*/

#import "CustomTableView.h"
#import "ControlTypes.h"

@implementation CustomTableView

@synthesize root;
@synthesize header;
@synthesize sharedConnection;
@synthesize shouldSendKeyStrokes;

-(id) initWithFrame:(NSRect)frameRect {
    self = [super initWithFrame:frameRect];
    if (self) {
        sharedConnection = [ClientHandler getConnection];
        shouldSendKeyStrokes = NO;
        keyPressCount = 1;
    }
    return self;
}

-(id) initWithFrame:(NSRect)frameRect Model:(Model*) _root header:(Model*) _header andContainer:(NSView*) container {
    self = [self initWithFrame:frameRect];
    if (self) {
        root = _root;
        header = _header;
        
        NSScrollView *scrollview = [[NSScrollView alloc] initWithFrame:frameRect] ;
        NSSize contentSize = [scrollview contentSize];
        
        [scrollview setBorderType:NSNoBorder];
        [scrollview setHasVerticalScroller:YES];
        [scrollview setHasHorizontalScroller:NO];
        [scrollview setAutoresizingMask:NSViewWidthSizable |
         NSViewHeightSizable];
        
        [self setFrame:NSMakeRect(0, 0, contentSize.width-2, contentSize.height)];

        [self setAutoresizingMask:NSViewWidthSizable];
        [self setRowHeight:30];
        [self setIntercellSpacing:NSMakeSize(3, 2)];

        // identifier
        [self setIdentifier:root.unique_id];
        
        //computing number of columns
        [self populateListHeader];
        
        
        [self setDelegate:(id<NSTableViewDelegate>) self];
        [self setDataSource:(id<NSTableViewDataSource>) self];
        
        //add to scrollView
        [scrollview setDocumentView:self];
        [container addSubview:scrollview];
        
    }
    
    //now load tableview
    listLoaded = NO;
    [self beginUpdates];
    [self reloadData];
    [self endUpdates];
    listLoaded = YES;
    NSLog(@"[sinter] - :%.0f: list_update", CACurrentMediaTime()*1000000000);
    return self;
}

- (void) updateWithRoot:(Model*) _root andHeader:(Model*) _header{
    header = _header;
    root = _root;
    //[self setIdentifier:root.unique_id];
    
    [self populateListHeader];

    //now load tableview
    listLoaded = NO;
    [self beginUpdates];
    [self reloadData];
    [self endUpdates];
    listLoaded = YES;
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

- (void)insertNewline:(id)sender{
    shouldForward = NO;
    [sharedConnection sendSpecialStroke:@"{ENTER}" numRepeat:keyPressCount];
}

- (void)deleteBackward:(id)sender {
    shouldForward = NO;
    [sharedConnection sendSpecialStroke:@"{BACKSPACE}" numRepeat:keyPressCount];
}

/*
- (void)insertText:(id)insertString {
    shouldForward = NO;
    // don't send it
    //[sharedConnection sendSpecialStroke:insertString numRepeat:keyPressCount];
}
*/

#pragma mark Render Method
- (void) addColumnFromUI:(Model*) cell{
    NSTableColumn* col = [[NSTableColumn alloc] initWithIdentifier:cell.name];
    [col setWidth: header.width / (1 + header.child_count)];
    [[col headerCell] setStringValue:cell.name];
    [[col dataCell] setControlSize:NSMiniControlSize];
    [self addTableColumn:col];
}

- (void) removeAllColumns{
    while([[self tableColumns] count] > 0) {
        [self removeTableColumn:[[self tableColumns] lastObject]];
    }
}

- (void) populateListHeader{
    [self removeAllColumns];
    
    if (!header.child_count) {
        [self addColumnFromUI:header];
        return;
    }
    
    // add new columns
    for (int i = 0; i < header.child_count ; i++) {
        Model* cell = header.children[i];
        [self addColumnFromUI:cell];
    }
}

#pragma mark NSTableViewDelegate methods
- (NSInteger) numberOfRowsInTableView:(NSTableView *) tableView {
    Model* listView = root;// [idTable objectForKey:tableView.identifier];
    if (!listView) return 0;
    return listView.child_count;
}

- (NSView *) tableView: (NSTableView *) tableView viewForTableColumn:(NSTableColumn *)tableColumn row:(NSInteger)row {
    NSTextField *result = [tableView makeViewWithIdentifier:tableColumn.identifier owner:self];
    //CustomTextField *result = [tableView makeViewWithIdentifier:tableColumn.identifier owner:self];
    if (result == nil) {
        result = [[NSTextField alloc] init];
        //result = [[CustomTextField alloc] init];
        [result setEditable:NO];
        result.identifier = tableColumn.identifier;
    }
    
    Model* listItem = root; //[idTable objectForKey:tableView.identifier];
    if (listItem && row < listItem.child_count) {
        listItem = listItem.children[row];
        // highlight selection
        if (listItem.states & STATE_SELECTED) {
            [tableView selectRowIndexes:[NSIndexSet indexSetWithIndex:row] byExtendingSelection:YES];
            listItem.states ^= STATE_SELECTED;
        }
        // if no column
        if(!listItem.child_count){
            result.stringValue = listItem.name;
            [result setIdentifier:listItem.unique_id];
            return result;
        }
        // add colomn data
        for (int i= 0; i < listItem.child_count; i++) {
            Model* col = listItem.children[i];
            if ([col.name isEqualToString:tableColumn.identifier]) {
                result.stringValue = col.value ? col.value : @"";
                [result setIdentifier:col.unique_id];
                [result sizeToFit];
                return result;
            }
        }
    }
    result.stringValue = @"Not Avaialable";
    return result;
}

//- (BOOL)tableView:(NSTableView *)aTableView shouldSelectRow:(NSInteger)rowIndex {}
//- (void) tableViewSelectionIsChanging:(NSNotification *) notification {}

- (void) tableViewSelectionDidChange:(NSNotification *) notification{
    if (!listLoaded) return;
    
    NSTableView *tableView = (NSTableView *)[notification object];
    NSInteger _selected = [tableView selectedRow];
    
    Model* list = root; //[idTable objectForKey:tableView.identifier];
    if (list && _selected < list.child_count) {
        Model* listItem = list.children[_selected];
        //NSTableCellView *selectedRow = [tableView viewAtColumn:0 row:selected makeIfNecessary:YES];
        //NSLog(@"tableViewSelectionIsChanging  %@",listItem.name);
        [sharedConnection sendFocusAt:listItem.unique_id];
    }
}

- (BOOL)tableView:(NSTableView *)aTableView shouldEditTableColumn:(NSTableColumn *)aTableColumn
              row:(NSInteger)rowIndex {
    return NO;
}


@end
