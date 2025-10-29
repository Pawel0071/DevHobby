#!/bin/bash
mongod --fork --logpath /var/log/mongodb.log
sleep 5
mongoimport --db game_config --collection tables --file /init/experience_levels.json --jsonArray --drop
mongoimport --db game_config --collection tables --file /init/class_stat_profiles.json --jsonArray --drop