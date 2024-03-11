# ASF Booster Creator Plugin

# ATTENTION! This plugin only works with ASF-generic!

# Introduction
This plugin was made by [Outzzz](https://github.com/Outzzz), but after some time it got removed from github. Luckily I forked it before that, and as it was published under Apache 2.0 license I can continue development of this plugin. I tried to improve it a little, you can check what changes I made to it in git commits history.<br/>
As title says, aim of this plugin is giving a user an easy way to create booster packs from gems, both by command and automatically.

## Installation
- download `BoosterCreator.zip` file from [latest release](https://github.com/Rudokhvist/BoosterCreator/releases/latest)
- unpack downloaded .zip file to `plugins` folder inside your ASF folder.
- (re)start ASF, you should get a message indicating that plugin loaded successfully. 

## Usage
There is two ways to create boosters with this plugin: manual and automatic.
To manually create booster just send ASF command `booster <bots> <appids>`, and ASF will try to create boosters from specified games on selected bots.<br/>
Example: `booster bot1 730`<br/>
To automatically create boosters you can add to config of your bot(s) parameter `GamesToBooster`, of type "array of uint". ASF will create boosters from specified games as long as there is enough gems, automatically waiting for cooldowns.<br/>
Example: `"GamesToBooster": [730, 570],`<br/>

![downloads](https://img.shields.io/github/downloads/Rudokhvist/BoosterCreator/total.svg?style=social)
