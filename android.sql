/*
Navicat MySQL Data Transfer

Source Server         : localhost
Source Server Version : 50505
Source Host           : localhost:3306
Source Database       : android

Target Server Type    : MYSQL
Target Server Version : 50505
File Encoding         : 65001

Date: 2019-11-24 00:46:52
*/

SET FOREIGN_KEY_CHECKS=0;

-- ----------------------------
-- Table structure for `accounts`
-- ----------------------------
DROP TABLE IF EXISTS `accounts`;
CREATE TABLE `accounts` (
  `id` int(11) DEFAULT NULL,
  `email` varchar(255) DEFAULT NULL,
  `password` varchar(255) DEFAULT NULL,
  `verified_number` int(11) DEFAULT NULL,
  `valid_account` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of accounts
-- ----------------------------
INSERT INTO `accounts` VALUES ('1', 'm.pop@gmail.com', '123456', '1', '1');

-- ----------------------------
-- Table structure for `submited_url`
-- ----------------------------
DROP TABLE IF EXISTS `submited_url`;
CREATE TABLE `submited_url` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `url` varchar(255) DEFAULT NULL,
  `loop` int(11) DEFAULT NULL,
  `rate` tinyint(4) DEFAULT NULL,
  `g` tinyint(4) DEFAULT NULL,
  `comment` tinyint(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of submited_url
-- ----------------------------
INSERT INTO `submited_url` VALUES ('1', 'https://play.google.com/store/apps/details?id=com.Seriously.Phoenix', '5', '5', '1', '0');
