
In UIChat.cs Search :

if (player)

Replace to :

if (player && settingsVariables.isShowChat)




------------------


In PlayerTrading.cs Search :

        return entity != null &&
               entity is Player other &&
               other != player &&
               CanStartTrade() &&
               other.trading.CanStartTrade() &&
               Utils.ClosestDistance(player, entity) <= player.interactionRange;


Replace to :


        return entity != null &&
               entity is Player other &&
               other != player &&
               CanStartTrade() &&
               other.trading.CanStartTrade() &&
               Utils.ClosestDistance(player, entity) <= player.interactionRange &&
                !((Player)entity).playerGameSettings.isBlockingTrade; 


-------------------
1 ) Add PlayerGameSettings component to player prefab

In PlayerGuild.cs Search :

        // validate
        if (player.target != null &&
            player.target is Player targetPlayer &&
            InGuild() && !targetPlayer.guild.InGuild() &&
            guild.CanInvite(name, targetPlayer.name) &&
            NetworkTime.time >= player.nextRiskyActionTime &&
            Utils.ClosestDistance(player, targetPlayer) <= player.interactionRange)


Replace to :


        // validate
        if (player.target != null &&
            player.target is Player targetPlayer &&
            InGuild() && !targetPlayer.guild.InGuild() &&
            guild.CanInvite(name, targetPlayer.name) &&
            NetworkTime.time >= player.nextRiskyActionTime &&
            Utils.ClosestDistance(player, targetPlayer) <= player.interactionRange &&
            !targetPlayer.playerGameSettings.isBlockingGuild)


-----------------------


In PlayerParty.cs Search :

            if ((!InParty() || !party.IsFull()) && !other.party.InParty())
Replace to :
            if ((!InParty() || !party.IsFull()) && !other.party.InParty() && !other.playerGameSettings.isBlockingParty)