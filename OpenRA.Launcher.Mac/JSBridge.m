/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "JSBridge.h"
#import "Controller.h"
#import "Download.h"
#import "Mod.h"

static JSBridge *SharedInstance;

@implementation JSBridge
@synthesize methods;

+ (JSBridge *)sharedInstance
{
	if (SharedInstance == nil)
		SharedInstance = [[JSBridge alloc] init];
	
	return SharedInstance;
}

+ (NSString *)webScriptNameForSelector:(SEL)sel
{
	return [[[JSBridge sharedInstance] methods] objectForKey:NSStringFromSelector(sel)];
}

+ (BOOL)isSelectorExcludedFromWebScript:(SEL)sel
{ 
	return [[[JSBridge sharedInstance] methods] objectForKey:NSStringFromSelector(sel)] == nil;
}

-(id)init
{
	self = [super init];
	if (self != nil)
	{
		methods = [[NSDictionary dictionaryWithObjectsAndKeys:
						@"launchMod", NSStringFromSelector(@selector(launchMod:)),
						@"log", NSStringFromSelector(@selector(log:)),
						@"existsInMod", NSStringFromSelector(@selector(fileExists:inMod:)),
						@"metadata", NSStringFromSelector(@selector(metadata:forMod:)),
						
						// File downloading
						@"registerDownload", NSStringFromSelector(@selector(registerDownload:withURL:filename:)),
						@"startDownload", NSStringFromSelector(@selector(startDownload:)),
						@"cancelDownload", NSStringFromSelector(@selector(cancelDownload:)),
						@"downloadStatus", NSStringFromSelector(@selector(downloadStatus:)),
						@"downloadError", NSStringFromSelector(@selector(downloadError:)),
						@"bytesCompleted", NSStringFromSelector(@selector(bytesCompleted:)),
						@"bytesTotal", NSStringFromSelector(@selector(bytesTotal:)),
						@"extractDownload", NSStringFromSelector(@selector(extractDownload:toPath:inMod:)),
					nil] retain];
	}
	return self;
}

- (void)setController:(Controller *)aController
{
	controller = [aController retain];
}

- (void)dealloc
{
	[controller release]; controller = nil;
	[super dealloc];
}

#pragma mark JS API methods

- (BOOL)launchMod:(NSString *)aMod
{
	// Build the list of mods to launch
	NSMutableArray *mods = [NSMutableArray array];
	NSString *current = aMod;
	
	// Assemble the mods in the reverse order to work around an engine bug
	while (current != nil)
	{
		Mod *mod = [[controller allMods] objectForKey:current];
		if (mod == nil)
		{
			NSLog(@"Unknown mod: %@", current);
			return NO;
		}
		[mods addObject:current];
		
		if ([mod standalone])
			current = nil;
		else
			current = [mod requires];
	}
	// Todo: Reverse the array ordering once the engine bug is fixed
	
	[controller launchMod:[mods componentsJoinedByString:@","]];
	return YES;
}

- (BOOL)registerDownload:(NSString *)key withURL:(NSString *)url filename:(NSString *)filename
{
	// Create the download directory if it doesn't exist
	NSString *downloadDir = [@"~/Library/Application Support/OpenRA/Downloads/" stringByExpandingTildeInPath];
	if (![[NSFileManager defaultManager] fileExistsAtPath:downloadDir])
		[[NSFileManager defaultManager] createDirectoryAtPath:downloadDir withIntermediateDirectories:YES attributes:nil error:NULL];
	
	// Disallow traversing directories; take only the last component
	NSString *path = [downloadDir stringByAppendingPathComponent:[filename lastPathComponent]];
	return [controller registerDownload:key withURL:url filePath:path];
}

- (NSString *)downloadStatus:(NSString *)key
{
	Download *d = [controller downloadWithKey:key];
	if (d == nil)
		return @"NOT_REGISTERED";
	
	return [d status];
}

- (NSString *)downloadError:(NSString *)key
{
	Download *d = [controller downloadWithKey:key];
	if (d == nil)
		return @"";
	
	return [d error];
}

- (BOOL)startDownload:(NSString *)key
{
	Download *d = [controller downloadWithKey:key];
	return (d == nil) ? NO : [d start];
}

- (BOOL)cancelDownload:(NSString *)key
{
	Download *d = [controller downloadWithKey:key];
	return (d == nil) ? NO : [d cancel];
}

- (int)bytesCompleted:(NSString *)key
{
	Download *d = [controller downloadWithKey:key];
	return (d == nil) ? -1 : [d bytesCompleted];
}

- (int)bytesTotal:(NSString *)key
{
	Download *d = [controller downloadWithKey:key];
	return (d == nil) ? -1 : [d bytesTotal];
}

- (void)notifyDownloadProgress:(Download *)download
{
	[[[controller webView] windowScriptObject] evaluateWebScript:
	 [NSString stringWithFormat:@"downloadProgressed('%@')",[download key]]];
}

- (void)notifyExtractProgress:(Download *)download
{
	[[[controller webView] windowScriptObject] evaluateWebScript:
	 [NSString stringWithFormat:@"extractProgressed('%@')",[download key]]];
}

- (BOOL)extractDownload:(NSString *)key toPath:(NSString *)aFile inMod:(NSString *)aMod
{
	Download *d = [controller downloadWithKey:key];
	if (d == nil)
	{
		NSLog(@"Unknown download");
		return NO;
	}
	if (![[d status] isEqualToString:@"DOWNLOADED"])
	{
		NSLog(@"Invalid download status");
		return NO;
	}
	
	id mod = [[controller allMods] objectForKey:aMod];
	if (mod == nil)
	{
		NSLog(@"Invalid or unknown mod: %@", aMod);
		return NO;
	}
	
	// Disallow traversing up the directory tree
	id path = [aMod stringByAppendingPathComponent:[aFile stringByReplacingOccurrencesOfString:@"../"
																			   withString:@""]];
	
	[d extractToPath:path];
	return YES;
}

- (void)log:(NSString *)message
{
	NSLog(@"js: %@",message);
}

- (BOOL)fileExists:(NSString *)aFile inMod:(NSString *)aMod
{
	id mod = [[controller allMods] objectForKey:aMod];
	if (mod == nil)
	{
		NSLog(@"Invalid or unknown mod: %@", aMod);
		return NO;
	}
	
	// Disallow traversing up the directory tree
	id path = [[mod path] stringByAppendingPathComponent:[aFile stringByReplacingOccurrencesOfString:@"../"
																			   withString:@""]];
	
	return [[NSFileManager defaultManager] fileExistsAtPath:path];
}

- (NSString *)metadata:(NSString *)aField forMod:(NSString *)aMod
{
	id mod = [[controller allMods] objectForKey:aMod];
	if (mod == nil)
	{
		NSLog(@"Invalid or unknown mod: %@", aMod);
		return @"";
	}
	
	if ([aField isEqualToString:@"VERSION"])
		return [mod version];
	
	NSLog(@"Invalid or unknown field: %@", aField);
	return @"";
}

@end
