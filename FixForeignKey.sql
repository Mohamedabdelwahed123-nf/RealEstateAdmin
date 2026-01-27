-- Script pour corriger la contrainte de clé étrangère
-- Exécutez ce script dans votre base de données MySQL

USE realestatedb;

-- Supprimer la contrainte existante
ALTER TABLE `biens` DROP FOREIGN KEY `FK_Biens_ApplicationUser_UserId`;

-- Recréer la contrainte avec ON DELETE SET NULL
ALTER TABLE `biens` 
ADD CONSTRAINT `FK_Biens_ApplicationUser_UserId` 
FOREIGN KEY (`UserId`) 
REFERENCES `ApplicationUser` (`Id`) 
ON DELETE SET NULL 
ON UPDATE CASCADE;
