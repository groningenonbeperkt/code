-- Wheelchair profile

api_version = 1

local find_access_tag = require("lib/access").find_access_tag
local Set = require('lib/set')
local Sequence = require('lib/sequence')
local Handlers = require("lib/handlers")
local Tags = require('lib/tags')
local next = next       -- bind to local for speed

properties.max_speed_for_map_matching    = 40/3.6 -- kmph -> m/s
properties.use_turn_restrictions         = false
properties.continue_straight_at_waypoint = false
--properties.weight_name                   = 'duration'
properties.weight_name                   = 'routability'


local walking_speed = 5

local profile = {
  default_mode            = mode.walking,
  default_speed           = walking_speed,
  oneway_handling         = 'specific',     -- respect 'oneway:foot' but not 'oneway'
  traffic_light_penalty   = 2,
  u_turn_penalty          = 2,
  restricted_penalty      = 300,

  barrier_whitelist = Set { -- wordt gebruikt voor nodes -> zie node_function
    'cycle_barrier',
    'bollard',
    'entrance',
    --'cattle_grid',   <- niet zeker
    'border_control',
    'toll_booth',
    'sally_port',
    'gate',
    'no',
    'block'
  },

  access_tag_whitelist = Set {
    'yes',
    'foot',
	'bicycle',
    'permissive',
    'designated'
  },

  access_tag_blacklist = Set { 
    'no',
    'agricultural',
    'forestry',
    'private',
    'delivery',
  },

  access_tags_hierarchy = Sequence {
  --'bicycle',
 -- 'vehicle',
    'foot',
    'access'
  },
  
  restricted_access_tag_list = Set { }, --'steps' },
  service_penalties = {},  -- <- wordt gevraagd in handlers zelf toegevoegd
  
  restricted_highway_whitelist = Set { -- (nog testen) bevat alle highway die gebruikt mogen worden
   -- 'primary',
   -- 'primary_link',
   -- 'secondary',
   -- 'secondary_link',
   -- 'tertiary',
   -- 'tertiary_link',
   -- 'residential',
   -- 'unclassified',
   -- 'road',
   -- 'living_street',
   -- 'service',
   -- 'path',
   -- 'pedestrian',
   -- 'footway'
  }, -- <- wordt gevraagd in handlers zelf toegevoegd

  highway_blacklist = Set {
    'motorway',
	'motorway_link',
	'trunk',
	'trunk_link',
	'steps',
	'track',
	'bus_guideway',
	'escape',
	'raceway',
	'bridleway',
	'construction'	
  },
  
  smoothness_blacklist = Set {
	'bad',
	'very_bad',
	'horrible',
	'very_horrible',
	'impassable'
  },
  
  surface_blacklist = Set {
    'ground',
	'gravel',
	'dirt',
	'grass',
	'sand',
	'wood',
	'earth',
	'pebblestone',
	'mud',
	'artificial_turf',
	'concrete:lanes'
  },
  
  kerb_blacklist = Set {
	'raised',
	'rolled',
	'yes'
  },
  
  ramp_blacklist = Set {
	'no',
	'yes',
	':stroller=yes',
	':bicycle=yes'
  },

  restrictions = Sequence {  -- voor wie is het eenrichtingsverkeer?
    'foot'
  },

  -- list of suffixes to suppress in name change instructions
  suffix_list = Set {
    'N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW', 'North', 'South', 'West', 'East'
  },

  avoid = Set {  -- uitbouwen gaat over wegen die niet gebruikt mogen worden.
    'impassable'--,
	--'steps'
  },

  surface_weight = { -- lager getal is vermijden groter
	  compacted       = 0.2,
	  cobblestone     = 0.3,
	  fine_gravel     = 0.8,
	  sett            = 0.8,
	  grass_paver     = 0.2
  },
  
  
  speeds = Sequence {
    highway = {
      primary         = walking_speed,
      primary_link    = walking_speed,
      secondary       = walking_speed,
      secondary_link  = walking_speed,
      tertiary        = walking_speed,
      tertiary_link   = walking_speed,
      unclassified    = walking_speed,
      residential     = walking_speed,
      road            = walking_speed,
      living_street   = walking_speed,
      service         = walking_speed,
      path            = walking_speed,
      pedestrian      = walking_speed,
      footway         = walking_speed,
	  cycleway        = walking_speed,
      pier            = walking_speed
    },

    railway = {
      platform        = walking_speed
    },

    amenity = {
      parking         = walking_speed,
      parking_entrance= walking_speed
    },

    man_made = {
      pier            = walking_speed
    },

    leisure = {
      track           = walking_speed
    }
  },

  route_speeds = {
    ferry = 5
  },

  bridge_speeds = {
  },

  surface_speeds = {
    --fine_gravel =   walking_speed*0.75,
    --gravel =        walking_speed*0.75,
    --pebblestone =   walking_speed*0.75,
    --mud =           walking_speed*0.5,
    --sand =          walking_speed*0.5
  },

  tracktype_speeds = {
  },

  smoothness_speeds = {
  }
}


function node_function (node, result)
  -- parse access and barrier tags
  local access = find_access_tag(node, profile.access_tags_hierarchy)
  if access then
    if profile.access_tag_blacklist[access] then
      result.barrier = true
    end
  else
    local barrier = node:get_value_by_key("barrier")
    if barrier then
      --  make an exception for rising bollard barriers
      local bollard = node:get_value_by_key("bollard")
      local rising_bollard = bollard and "rising" == bollard

      if not profile.barrier_whitelist[barrier] and not rising_bollard then
        result.barrier = true
      end
    end
  end

  -- check if node is a traffic light
  local tag = node:get_value_by_key("highway")
  if "traffic_signals" == tag then
    result.traffic_lights = true
  end
end

-- main entry point for processsing a way
function way_function(way, result)
  -- the intial filtering of ways based on presence of tags
  -- affects processing times significantly, because all ways
  -- have to be checked.
  -- to increase performance, prefetching and intial tag check
  -- is done in directly instead of via a handler.

  -- in general we should  try to abort as soon as
  -- possible if the way is not routable, to avoid doing
  -- unnecessary work. this implies we should check things that
  -- commonly forbids access early, and handle edge cases later.

  -- data table for storing intermediate values during processing
  local data = {
    -- prefetch tags
    highway = way:get_value_by_key('highway'),
    bridge = way:get_value_by_key('bridge'),
    route = way:get_value_by_key('route'),
    leisure = way:get_value_by_key('leisure'),
    man_made = way:get_value_by_key('man_made'),
    railway = way:get_value_by_key('railway'),
    platform = way:get_value_by_key('platform'),
    amenity = way:get_value_by_key('amenity'),
    public_transport = way:get_value_by_key('public_transport'),
	wheelchair = way:get_value_by_key('wheelchair')
  }

  -- perform an quick initial check and abort if the way is
  -- obviously not routable. here we require at least one
  -- of the prefetched tags to be present, ie. the data table
  -- cannot be empty
  if next(data) == nil then     -- is the data table empty?
    return
  end

  local surface = way:get_value_by_key('surface')
  --if not data.wheelchair == 'yes' then
	if profile.highway_blacklist[data.highway] then
		return 
	end
	
	if profile.surface_blacklist[surface] then
		return false
	end
   
	local smoothness = way:get_value_by_key('smoothness')
	if profile.smoothness_blacklist[smoothness]  then
		return false
	end
	
	local ramp = way:get_value_by_key('ramp')
	if profile.ramp_blacklist[ramp]  then
		return false
	end
	
	local kerb = way:get_value_by_key('kerb')
	if profile.kerb_blacklist[kerb]  then
		return false
	end
		
	local width_string = way:get_value_by_key("width")
	if width_string and tonumber(width_string:match("%d*")) then
		width = tonumber(width_string:match("%d*"))
		if width <= 0.9 then
			return false
		end
	end
  --end
  
  local handlers = Sequence {
    -- set the default mode for this profile. if can be changed later
    -- in case it turns we're e.g. on a ferry
    'handle_default_mode',

    -- check various tags that could indicate that the way is not
    -- routable. this includes things like status=impassable,
    -- toll=yes and oneway=reversible
    'handle_blocked_ways',

    -- determine access status by checking our hierarchy of
    -- access tags, e.g: motorcar, motor_vehicle, vehicle
    'handle_access',

    -- check whether forward/backward directons are routable
    'handle_oneway',

    -- check whether forward/backward directons are routable
    'handle_destinations',

    -- check whether we're using a special transport mode
    'handle_ferries',
    'handle_movables',

    -- compute speed taking into account way type, maxspeed tags, etc.
    'handle_speed',
    'handle_surface',
    'handle_penalties',

    -- handle turn lanes and road classification, used for guidance
    'handle_classification',

    -- handle various other flags
    'handle_roundabouts',
    'handle_startpoint',

    -- set name, ref and pronunciation
    'handle_names'
  }

  Handlers.run(handlers,way,result,data,profile)
   
   local test_penalty  = 1
   
  for value,t in pairs(profile.surface_weight) do
    if surface == value then
	test_penalty = t
	end
  end
  
   
  if properties.weight_name == 'routability' then
    if result.forward_speed > 0 then
      result.forward_rate = (result.forward_speed * test_penalty) / 3.6
    end
    if result.backward_speed > 0 then
      result.backward_rate = (result.backward_speed * test_penalty) / 3.6
    end
    if result.duration > 0 then
      result.weight = result.duration / test_penalty
    end
  end

end

function turn_function (turn)
  turn.duration = 0.

  if turn.direction_modifier == direction_modifier.u_turn then
     turn.duration = turn.duration + profile.u_turn_penalty
  end

  if turn.has_traffic_light then
     turn.duration = profile.traffic_light_penalty
  end
  if properties.weight_name == 'routability' then
       --penalize turns from non-local access only segments onto local access only tags
      if not turn.source_restricted and turn.target_restricted then
         turn.weight = turn.weight + profile.restricted_penalty
      end
  end
end
