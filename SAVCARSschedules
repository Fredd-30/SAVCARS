<?php
/**
 * phpVMS - Virtual Airline Administration Software
 * Copyright (c) 2008 Nabeel Shahzad
 * For more information, visit www.phpvms.net
 *	Forums: http://www.phpvms.net/forum
 *	Documentation: http://www.phpvms.net/docs
 *
 * phpVMS is licenced under the following license:
 *   Creative Commons Attribution Non-commercial Share Alike (by-nc-sa)
 *   View license.txt in the root, or visit http://creativecommons.org/licenses/by-nc-sa/3.0/
 *
 * @license http://creativecommons.org/licenses/by-nc-sa/3.0/
 * @Modified by Bradley De-Lar 2013
 */

class SAVCARSschedules extends CodonModule
{
	public function index()
	{


		$allroutes = SchedulesData::GetSchedules();
		$TotalSchedules = StatsData::TotalSchedules();
		$count = 0;
		/////XML/////
		$xml = new SimpleXMLElement("<schedules />");
		$xml->addAttribute('count', $TotalSchedules);

			foreach($allroutes as $route)
			{
				if ($count >= $TotalSchedules){
				break;
				}
				

					$route->daysofweek = str_replace('7', '0', $route->daysofweek);
					$aircraftinfo = OperationsData::getAircraftByReg($route->registration);
					$distance = number_format($route->distance);
					$distance = str_replace(",", "", $distance);

						
						$info_xml = $xml->addChild('schedule');
							$info_xml->addChild('flightnum', $route->code . $route->flightnum);
							$info_xml->addChild('depicao', $route->depicao);
							$info_xml->addChild('arricao', $route->arricao);
							$info_xml->addChild('type', $route->flighttype);
							$info_xml->addChild('deptime', $route->deptime);
							$info_xml->addChild('arrtime', $route->arrtime);
							$info_xml->addChild('aircraft', $aircraftinfo->icao);
							$info_xml->addChild('registration', $route->registration);
							$info_xml->addChild('distance', $distance . Config::Get('UNITS'));
							$info_xml->addChild('route', ($route->route=='') ? '' : $route->route);
							$info_xml->addChild('dow', $route->daysofweek);
						
						
				$count++;
				

			}
			header('Content-type: text/xml');
			echo $xml->asXML();

	}
	

}
