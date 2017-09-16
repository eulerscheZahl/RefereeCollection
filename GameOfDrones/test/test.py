#!/usr/bin/python3

import os, json

player_count = 0
zones = []
drone_count = 0

def parse_frame(frames, index):
	global player_count, zones, drone_count
	
	frame = frames[index]
	view = frame['view'].strip('\n').split('\n')
	turn = int(view[0])
	if turn == 0:
		player_count, _, drone_count, zone_count = map(int, view[3].split())
		zones = view[4:4+zone_count]
		view = view[4 + zone_count:]
	else:
		view = view[1:]

	stdin = ['###Input ' + str((frame['agentId']+1)%player_count)]
	if index < player_count:
		stdin.append('{} {} {} {}'.format(player_count, (frame['agentId']+1)%player_count, drone_count, len(zones)))
		for zone in zones: stdin.append(zone)
	for i in range(len(zones)):
		stdin.append(view[player_count + i*(player_count+1)]) # get the owners, skip how many drones are in a zone
	view = view[(player_count+1) * len(zones) + player_count:]
	for drone in view:
		stdin.append(' '.join(drone.split()[:2]))

	if 'stdout' in frame:
		stdout = frame['stdout'].strip('\n').split('\n')
	if index > 0:
		stdin.insert(0, '###Output {} {}'.format(frame['agentId'], drone_count))
	if 'stdout' not in frame:
		stdout = ['###Map']
		for i in stdin:
			if '###' not in i: stdout.append(i)
		stdout.append('###Start ' + str(player_count))
	return {'stdin':stdin, 'stdout':stdout}


for filename in [pos_json for pos_json in os.listdir('.') if pos_json.endswith('.json')]:
	with open(filename, 'r') as f:
		data = json.load(f)
	frames = data['success']['frames']
	initial = parse_frame(frames, 0)
	if len(frames) != 200 * player_count + 1:
		continue # my referee doesn't handle crashing players, just go on with the next testfile
		
	with open(os.path.splitext(filename)[0] + '.in', 'w') as stdin, \
	     open(os.path.splitext(filename)[0] + '.out', 'w') as stdout:
		for index in range(len(frames)):
			for text in  parse_frame(frames, index)['stdin']: 
				stdin.write(text + '\n')
			for text in  parse_frame(frames, index)['stdout']: 
				stdout.write(text + '\n')
				
# compare files to check correctness:
# e.g. ./GameOfDrones.exe < 256965558.out > mySim.in && diff 256965558.in mySim.in 
# there are a few differences when a drone has a distance of nearly exactly 100 => rounding errors
			
