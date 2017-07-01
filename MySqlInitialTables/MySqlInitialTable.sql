-- MySQL dump 10.13  Distrib 5.5.9, for Win32 (x86)
--
-- Host: localhost    Database: geopath
-- ------------------------------------------------------
-- Server version	5.5.15

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
--
-- Current Database: `some_geopath`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `some_geopath` /*!40100 DEFAULT CHARACTER SET latin1 */;

USE `geopath`;

--
-- Table structure for table `geopath`
--

DROP TABLE IF EXISTS `geopath`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `geopath` (
  `nId` int(11) NOT NULL AUTO_INCREMENT,
  `sOwnerId` varchar(255) DEFAULT NULL,
  `eShare` tinyint(1) NOT NULL DEFAULT '0',
  `sName` varchar(255) DEFAULT NULL,
  `latBegin` double DEFAULT NULL,
  `lonBegin` double DEFAULT NULL,
  `latEnd` double DEFAULT NULL,
  `lonEnd` double DEFAULT NULL,
  `latSW` double DEFAULT NULL,
  `lonSW` double DEFAULT NULL,
  `latNE` double DEFAULT NULL,
  `lonNE` double DEFAULT NULL,
  `tModified` datetime DEFAULT NULL,
  `xmlData` longtext,
  `bDeleted` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`nId`)
) ENGINE=InnoDB AUTO_INCREMENT=68 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `owner`
--

DROP TABLE IF EXISTS `owner`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `owner` (
  `nId` int(11) NOT NULL AUTO_INCREMENT,
  `sOwnerId` varchar(255) DEFAULT NULL,
  `sName` varchar(255) DEFAULT NULL,
  `accessHandle` varchar(255) DEFAULT NULL,
  `tModified` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`nId`),
  UNIQUE KEY `OWNER` (`sOwnerId`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=latin1 COMMENT='Info about owner of geopath records.';
/*!40101 SET character_set_client = @saved_cs_client */;

/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2017-07-01 12:36:44
