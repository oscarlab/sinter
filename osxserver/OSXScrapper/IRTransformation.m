//
//  IRTransformation.m
//  OSXScrapper
//
//  Created by Syed Masum Billah on 10/21/15.
//  Copyright (c) 2015 Stony Brook University. All rights reserved.
//

#import "IRTransformation.h"
#import "XMLParser.h"

@implementation IRTransformation



+(NSXMLNode *) transformAsTreeNode:(NSXMLNode *) app andReturnSelectedIndex:(int *) index{
    NSXMLNode* list;
    
    if (![[app localName] isEqualToString:@"AXScrollArea"]){
        *index = -1;
        [app detach];
        return app;
    }
    
    NSArray* temp =  [XMLParser getChildrenAsArray:app];
    if (!temp && !temp.count) return app;
    for (NSXMLNode* item in temp) {
        if ([[item localName] isEqualToString:@"AXList"]) {
            list = item;
        }else{
            [item detach];
        }
    }
    if (!list) return app;
    app = [XMLParser moveChildren:list andParentNode:app];
    [app setName:@"TreeNode"];
    
    NSArray * items = [XMLParser getChildrenAsArray:app]; //AXGroup
    int i = 0;
    for (NSXMLNode* item in items) {
        NSXMLNode * elem = [[XMLParser getChildrenAsArray:item] firstObject]; //StaticText
        NSString* nodeName = [XMLParser getAttributeByName:elem andName:@"value"];
        [elem detach]; // delete
        
        [item setName:@"TreeNode"]; //change group to TreeNode
        [XMLParser setAttribute:item andName:@"name" andValue:nodeName];
        if (!i) {
            [XMLParser setAttribute:item andName:@"selected" andValue:@"yes"];
            *index = i;
        }else{
            [XMLParser setAttribute:item andName:@"selected" andValue:@"no"];
        }
        i++;
    }
    return app;
}


+(NSXMLNode *) transformAsListItem:(NSXMLNode *) app{
    NSXMLNode* list;
    NSArray* temp =  [XMLParser getChildrenAsArray:app];
    if (!temp && !temp.count) return app;
    for (NSXMLNode* item in temp) {
        if ([[item localName] isEqualToString:@"AXList"]) {
            list = item;
        }else{
            [item detach];
        }
    }
    if (!list) return app;
    app = [XMLParser moveChildren:list andParentNode:app];
    
    NSArray * items = [XMLParser getChildrenAsArray:app]; //AXGroup
    for (NSXMLNode* item in items) {
        NSXMLNode * elem = [[XMLParser getChildrenAsArray:item] firstObject]; //StaticText
        NSString* nodeName = [XMLParser getAttributeByName:elem andName:@"value"];
        [elem detach]; // delete
        
        [item setName:@"ListViewItem"]; //change group to ListViewItem
        [XMLParser setAttribute:item andName:@"name" andValue:nodeName];
    }
    return app;
}


+(NSXMLNode *) transformOutlineViewAsTreeNode:(NSXMLNode *) app{
    NSXMLNode* localParent, *globalParent, *item;
    NSArray* items =  [XMLParser getChildrenAsArray:app];
    if (!items && !items.count) return app;
    int i, j;

    globalParent = app;
    localParent = (NSXMLNode*)[items firstObject];
    j = -1;
    for (i=0; i< items.count; i++) { //AXRow
        item = (NSXMLNode*)items[i];
        if (![[item localName] isEqualToString:@"AXRow"]) {
            [item detach];
            i++;
            continue;
        }
        NSXMLNode * elem = [[XMLParser getChildrenAsArray:item] firstObject]; //StaticText
        NSString* nodeName = [XMLParser getAttributeByName:elem andName:@"value"];
        [elem detach]; // delete
        
        [item setName:@"TreeNode"];
        [XMLParser setAttribute:item andName:@"name" andValue:nodeName];
        
        if (i <= j) {
            [item detach];
            [XMLParser addNode:item andDestination:localParent];
        }else{
            localParent = item;
            j +=[[XMLParser getAttributeByName:item andName:@"num_child"] intValue];
            j++;
        }
    }
    [globalParent setName:@"TreeNode"];
    return globalParent;
}


+(NSXMLNode *) lookAlike:(NSXMLNode *) app{
    NSXMLNode* treeView;
    NSXMLNode* listView;
    NSXMLNode* outlineView;

    //remove desktop reference
    NSArray* temp =  [XMLParser getNodesAt:app byName:@"./children/AXScrollArea"];
    if (!temp && !temp.count) return app;
    [[temp firstObject] detach];

    //remove static text
    temp =  [XMLParser getNodesAt:app byName:@"./children/AXWindow/children/AXStaticText"];
    if (!temp && !temp.count) return app;
    [[temp firstObject] detach];

    // remove system buttons
    temp =  [XMLParser getNodesAt:app byName:@"./children/AXWindow/children/AXButton"];
    if (!temp && !temp.count) return app;
    for (NSXMLNode* item in temp)
        [item detach]; //delete

    
    // remove toolbar buttons
    NSXMLNode* toolbar;
    temp =  [XMLParser getNodesAt:app byName:@"./children/AXWindow/children/AXToolbar"];
    if (!temp && !temp.count) return app;
    toolbar = [temp firstObject];
    
    NSXMLNode* menuButton[2];
    int j = 0;
    for (int i=3; i < 5; i++) {
        temp =  [XMLParser getNodesAt:app byName:[NSString stringWithFormat:@"./children/AXWindow/children/AXToolbar/children/AXGroup[%i]", i]];
        if (!temp && !temp.count) return app;
        menuButton[j++] = [temp firstObject];
    }
    for (j=0; j< 2; j++) {
        toolbar = [XMLParser moveChildren:menuButton[j] andParentNode:toolbar];
    }


    // remove radioGroup
    NSXMLNode* radioGroup;
    temp =  [XMLParser getNodesAt:app byName:@"./children/AXWindow/children/AXToolbar/children/AXGroup/children/AXRadioGroup"];
    if (!temp && !temp.count) return app;
    radioGroup = [temp firstObject];
    [radioGroup setName:@"AXGroup"];
    
    for (NSXMLNode* item in [XMLParser getChildrenAsArray:radioGroup])
        [item setName:@"AXButton"];

    // search-box button
    temp =  [XMLParser getNodesAt:toolbar byName:@"./children/AXGroup/children/AXTextField/children/AXButton"];
    if (!temp && !temp.count) return app;
    [[temp firstObject] detach];

    
    // get treeview
    temp =  [XMLParser getNodesAt:app byName:@"./children/AXWindow/children/AXSplitGroup/children/AXScrollArea"];
    if (!temp && !temp.count) return app;
    treeView = [temp firstObject];

    // get list view
    temp =  [XMLParser getNodesAt:app byName:@"./children/AXWindow/children/AXSplitGroup/children/AXSplitGroup"];
    if (!temp && !temp.count) return app;
    listView = [temp firstObject];

    // get outlineview
    temp =  [XMLParser getNodesAt:app byName:@"./children/AXWindow/children/AXSplitGroup/children/AXScrollArea/children/AXOutline"];
    if (!temp && !temp.count) return app;
    outlineView = [temp firstObject];
    [outlineView detach];
    
    // get items for tree and list views
    NSXMLNode* items;
    temp =  [XMLParser getNodesAt:listView byName:@"./children/AXBrowser/children/AXScrollArea"];
    if (!temp && !temp.count) return app;
    items = [temp firstObject];
    
    // copy data for tree view
    NSArray * itemsToMove = [XMLParser getChildrenAsArray:items];
    int numItemsToMove = (int)itemsToMove.count;
    
    // copy data for list view
    NSXMLNode* listViewItem =  [[itemsToMove lastObject] copy];

    // process data for tree view
    int selected = -1;
    NSXMLNode* treeNodeRoot = [self transformAsTreeNode:[itemsToMove firstObject] andReturnSelectedIndex:&selected];

    NSXMLNode* toAppend = nil;
    if (selected >=0 ) {
        toAppend = (NSXMLNode*)[XMLParser getChildrenAsArray:treeNodeRoot][selected];
    }

    NSXMLNode* treeNode, *tempNode ;
    for (int i = 1; i<numItemsToMove; i++) {
        treeNode = (NSXMLNode*) itemsToMove[i];
        treeNode = [self transformAsTreeNode:treeNode andReturnSelectedIndex:&selected];
        if (selected < 0) {
            continue;
        }
        tempNode = (NSXMLNode*)[XMLParser getChildrenAsArray:treeNode][selected];
        if (toAppend) {
            [XMLParser moveChildren:treeNode andParentNode:toAppend];
            toAppend = tempNode;
        }
    }
    [treeNodeRoot detach];
    [XMLParser setAttribute:treeNodeRoot andName:@"name" andValue:@"Documents"];

    // drop all existing children
    treeView = [XMLParser dropAllChildren:treeView];
    
    // add outline view as favorites
    outlineView = [self transformOutlineViewAsTreeNode:outlineView];
    [XMLParser addNode:outlineView andDestination:treeView];

    // add file-folders
    [XMLParser addNode:treeNodeRoot andDestination:treeView];
    [treeView setName:@"TreeNode"];
    [XMLParser setAttribute:treeView andName:@"name" andValue:@"FileSystem"];

    // now process data for list view
    listViewItem =  [self transformAsListItem:listViewItem];
    [listViewItem detach];
    listView =  [XMLParser dropAllChildren:listView];
    [XMLParser moveChildren:listViewItem andParentNode:listView];
    [listView  setName:@"AXList"];
    [XMLParser setAttribute:treeView andName:@"name" andValue:@"File/Folders"];
    
    return app;
}


+(NSXMLNode *) performMenuTransfromation2On:(NSXMLNode *) app{
    NSXMLNode* window;
    NSXMLNode* menubar;

    NSArray* temp =  [XMLParser getNodesAt:app byName:@"./children/AXWindow/children"];
    if (!temp && !temp.count) return app;
    window = [temp firstObject];
    
    temp = [XMLParser getNodesAt:app byName:@"./children/AXMenuBar"];
    if (!temp && !temp.count) return app;
    
    menubar = [temp firstObject];
    [menubar detach];
    [(NSXMLElement*) window addChild:menubar];

    return app;
}

+(NSXMLNode *) performMenuTransfromationOn:(NSXMLNode *) app {

    NSArray* children =  [XMLParser getChildrenAsArray:app];
    if (!children) {
        return nil;
    }
    
    for (NSXMLNode* child in children) {
        if ([[child localName] isEqualToString:@"AXMenu"]) {
            app = [XMLParser moveChildren:child andParentNode:app];
        }
    }
    
    children =  [XMLParser getChildrenAsArray:app];
    if (!children) {
        return nil;
    }
    for (NSXMLNode* child in children) {
        [self performMenuTransfromationOn:child];
    }
    return app;
}

@end
