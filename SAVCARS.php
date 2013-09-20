<?php
//phpinfo();
//echo memory_get_usage() . "-1";
//echo "<br />";
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
 * @Original author Jeffery Kobus
 * @Original copyright Copyright (c) 2010, Jeffery Kobus
 * @link http://www.fs-products.net
 * @license http://creativecommons.org/licenses/by-nc-sa/3.0/
 * @Modified by Bradley De-Lar 2013
 */



class SAVCARS extends CodonModule
{
	public function index()
	{
		if($_SERVER['REQUEST_METHOD'] === 'POST')
		{ 
			$postText = file_get_contents('php://input');		
			
			$rec_xml = trim(utf8_encode(file_get_contents('php://input')));
			$xml = simplexml_load_string($rec_xml);	
			
			if(!$xml)
			{
				#$this->log("Invalid XML Sent: \n".$rec_xml, 'kacars');
				return;	
			}
			
			#$this->log(print_r($xml->asXML(), true), 'kacars');
			
			$case = strtolower($xml->switch->data);
			switch($case)
			{
				case 'verify':	
				case 'test':
				$ID = $xml->verify->pilotID;
					$results = Auth::ProcessLogin($ID, $xml->verify->password);	
					
					$ID = preg_replace("/[^0-9]/", '', $ID);
					$VATSIMID = PilotData::getFieldValue($ID,'VATSIM_PID');
					$ScheduleDown = PilotData::getFieldValue($ID,'Schedule_Download');
					$latestversion = file_get_contents('http://www.alphastone.co.uk/severnair/phpvms/core/modules/SAVCARS/latestversion.txt');
					if ($results and $xml->verify->version == $latestversion)
					{							
						$params = array('loginStatus' => '1',
						'VATSIMID' => (empty($VATSIMID) ? '0' : $VATSIMID),
						'Schedule_Download' => (empty($ScheduleDown) ? '0' : $ScheduleDown)
						);
						
						
						
						//echo 1;
					}
					else
					{
						$params = array('loginStatus' => '0',
						'VATSIMID' => '0',
						'Schedule_Download' => '0'
						);
						//echo 0;
					}
					
					$send = self::sendXML($params);
					
					break;
				
				case 'getbid':							
					
					$pilotid = PilotData::parsePilotID($xml->verify->pilotID);
					$pilotinfo = PilotData::getPilotData($pilotid);
					$biddata = SchedulesData::getLatestBid($pilotid);
					$aircraftinfo = OperationsData::getAircraftByReg($biddata->registration);
					
					if(count($biddata) == 1)
					{		
						if($aircraftinfo->enabled == 1)
						{
							$params = array(
								'flightStatus' 	   => '1',
								'flightNumber'     => $biddata->code.$biddata->flightnum,
								'aircraftReg'      => $biddata->registration,
								'aircraftICAO'     => $aircraftinfo->icao,
								'aircraftFullName' => $aircraftinfo->fullname,
								'flightLevel'      => $biddata->flightlevel,
								'aircraftMaxPax'   => $aircraftinfo->maxpax,
								'aircraftCargo'    => $aircraftinfo->maxcargo,
								'depICAO'          => $biddata->depicao,
								'arrICAO'          => $biddata->arricao,
								'route'            => $biddata->route,
								'depTime'          => $biddata->deptime,
								'arrTime'          => $biddata->arrtime,
								'flightTime'       => $biddata->flighttime,
								'flightType'       => $biddata->flighttype,
								'aircraftName'     => $aircraftinfo->name,
								'aircraftRange'    => $aircraftinfo->range,
								'aircraftWeight'   => $aircraftinfo->weight,
								'aircraftCruise'   => $aircraftinfo->cruise
								);					
						}
						else
						{	
							$params = array(
								'flightStatus' 	   => '3');		// Aircraft Out of Service.							
						}			
					}		
					else		
					{
						$params = array('flightStatus' => '2');	// You have no bids!								
					}
					
					$send = $this->sendXML($params);
					
					break;
				
				case 'getflight':
					
					$flightinfo = SchedulesData::getProperFlightNum($xml->pirep->flightNumber);
					
					$params = array(
						's.code' => $flightinfo['code'],
						's.flightnum' => $flightinfo['flightnum'],
						's.enabled' => 1,
					);
					
					$biddata = SchedulesData::findSchedules($params, 1);
					$aircraftinfo = OperationsData::getAircraftByReg($biddata[0]->registration);
					
					if(count($biddata) == 1)
					{		
						$params = array(
							'flightStatus' 	   => '1',
							'flightNumber'     => $biddata[0]->code.$biddata[0]->flightnum,
							'aircraftReg'      => $biddata[0]->registration,
							'aircraftICAO'     => $aircraftinfo->icao,
							'aircraftFullName' => $aircraftinfo->fullname,
							'flightLevel'      => $biddata[0]->flightlevel,
							'aircraftMaxPax'   => $aircraftinfo->maxpax,
							'aircraftCargo'    => $aircraftinfo->maxcargo,
							'depICAO'          => $biddata[0]->depicao,
							'arrICAO'          => $biddata[0]->arricao,
							'route'            => $biddata[0]->route,
							'depTime'          => $biddata[0]->deptime,
							'arrTime'          => $biddata[0]->arrtime,
							'flightTime'       => $biddata[0]->flighttime,
							'flightType'       => $biddata[0]->flighttype,
							'aircraftName'     => $aircraftinfo->name,
							'aircraftRange'    => $aircraftinfo->range,
							'aircraftWeight'   => $aircraftinfo->weight,
							'aircraftCruise'   => $aircraftinfo->cruise
							);
					}			
					else		
					{	
						$params = array('flightStatus' 	   => '2');								
					}
					
					$send = $this->sendXML($params);
					break;			
				
				case 'liveupdate':	
					
					$pilotid = PilotData::parsePilotID($xml->verify->pilotID);
					
					# Get the distance remaining
					$depapt = OperationsData::GetAirportInfo($xml->liveupdate->depICAO);
					$arrapt = OperationsData::GetAirportInfo($xml->liveupdate->arrICAO);
					$dist_remain = round(SchedulesData::distanceBetweenPoints(
						$xml->liveupdate->latitude, $xml->liveupdate->longitude, 
						$arrapt->lat, $arrapt->lng));
					
					# Estimate the time remaining
					if($xml->liveupdate->groundSpeed > 0)
					{
						$Minutes = round($dist_remain / $xml->liveupdate->groundSpeed * 60);
						$time_remain = self::ConvertMinutes2Hours($Minutes);
					}
					else
					{
						$time_remain = '00:00';
					}		
					
					$lat = str_replace(",", ".", $xml->liveupdate->latitude);
					$lon = str_replace(",", ".", $xml->liveupdate->longitude);
					
					$fields = array(
						'pilotid'        =>$pilotid,
						'flightnum'      =>$xml->liveupdate->flightNumber,
						'pilotname'      =>'',
						'aircraft'       =>$xml->liveupdate->registration,
						'lat'            =>$lat,
						'lng'            =>$lon,
						'heading'        =>$xml->liveupdate->heading,
						'alt'            =>$xml->liveupdate->altitude,
						'gs'             =>$xml->liveupdate->groundSpeed,
						'depicao'        =>$xml->liveupdate->depICAO,
						'arricao'        =>$xml->liveupdate->arrICAO,
						'deptime'        =>$xml->liveupdate->depTime,
						'arrtime'        =>'',
						'route'          =>$xml->liveupdate->route,
						'distremain'     =>$dist_remain,
						'timeremaining'  =>$time_remain,
						'phasedetail'    =>$xml->liveupdate->status,
						'online'         =>'',
						'client'         =>'kACARS',
						);
					
					#$this->log("UpdateFlightData: \n".print_r($fields, true), 'kacars');
					ACARSData::UpdateFlightData($pilotid, $fields);	
					
					break;
				
				case 'pirep':						
					
					$flightinfo = SchedulesData::getProperFlightNum($xml->pirep->flightNumber);
					$code = $flightinfo['code'];
					$flightnum = $flightinfo['flightnum'];
					
					$pilotid = PilotData::parsePilotID($xml->verify->pilotID);
					
					# Make sure airports exist:
					#  If not, add them.
					
					if(!OperationsData::GetAirportInfo($xml->pirep->depICAO))
					{
						OperationsData::RetrieveAirportInfo($xml->pirep->depICAO);
					}
					
					if(!OperationsData::GetAirportInfo($xml->pirep->arrICAO))
					{
						OperationsData::RetrieveAirportInfo($xml->pirep->arrICAO);
					}
					
					# Get aircraft information
					$reg = trim($xml->pirep->registration);
					$ac = OperationsData::GetAircraftByReg($reg);
					
					# Load info
					/* If no passengers set, then set it to the cargo */
					$load = $xml->pirep->load;

					
					/* Fuel conversion - kAcars only reports in lbs */
					$fuelused = $xml->pirep->fuelUsed;
					if(Config::Get('LiquidUnit') == '0')
					{
						# Convert to KGs, divide by density since d = mass * volume
						$fuelused = ($fuelused * .45359237) / .8075;
					}
					# Convert lbs to gallons
					elseif(Config::Get('LiquidUnit') == '1')
					{
						$fuelused = $fuelused * 6.84;
					}
					# Convert lbs to kgs
					elseif(Config::Get('LiquidUnit') == '2')
					{
						$fuelused = $fuelused * .45359237;
					}

					$log = $xml->pirep->log;		
					//$log = strtr($log, '\n','\\n');
					//$log = substr($log, 0, -1);
					
					$data = array(
						 'pilotid'=>$pilotid,
						'code'=>$code,
						'flightnum'=>$flightnum,
						'depicao'=>$xml->pirep->depICAO,
						'arricao'=>$xml->pirep->arrICAO,
						'aircraft'=>$ac->id,
						'flighttime'=>$xml->pirep->flightTime,
						'submitdate'=>'NOW()',
						'comment'=>$xml->pirep->comments,
						'fuelused'=>$fuelused,
						'source'=>'SAVCARS',
						'load'=>$load,
						'landingrate'=>$xml->pirep->landing,
						'log'=>$log
					);
					
					#$this->log("File PIREP: \n".print_r($data, true), 'kacars');
					$ret = ACARSData::FilePIREP($pilotid, $data);		
					
					if ($ret)
					{
						$params = array(
							'pirepStatus' 	   => '1');	// Pirep Filed!							
					}
					else
					{
						$params = array(
							'pirepStatus' 	   => '2');	// Please Try Again!							
						
					}
					$send = $this->sendXML($params);						
					
					break;	
				
				case 'aircraft':
					
					$this->getAllAircraft();
					break;
					
				case 'vatsim':
					$this->create_vatsim_data('!CLIENTS:','PILOT', $xml->pirep->ID);
					break;
					
				case 'versioncheck':
					
					$this->versioncheck($xml->version->versionnumber);
					break;
					
				Case 'schedules':
				
					$ID = preg_replace("/[^0-9]/", '', $xml->verify->pilotID);
					PilotData::saveFields($ID, array('SCHEDULE_DOWNLOAD'=>'False'));
					break;
					
				case 'getdistance':
				
						if(!OperationsData::GetAirportInfo($xml->airports->depICAO))
						{
							OperationsData::RetrieveAirportInfo($xml->airports->depICAO);
						}
						
						if(!OperationsData::GetAirportInfo($xml->airports->arrICAO))
						{
							OperationsData::RetrieveAirportInfo($xml->airports->arrICAO);
						}
						
					$depapt = OperationsData::GetAirportInfo($xml->airports->depICAO);
					$arrapt = OperationsData::GetAirportInfo($xml->airports->arrICAO);
					
					$params = array(
					'deplat' 	   => $depapt->lat,
					'deplon' 	   => $depapt->lng,
					'arrlat' 	   => $arrapt->lat,
					'arrlon' 	   => $arrapt->lng
					
					);							
						
					
					$send = $this->sendXML($params);
						
					break;
					
				case 'vatsimcheck':
					$this->vatsimonlinecheck('!CLIENTS:','PILOT', $xml->vatsim->VATSIMID);
					
					break;
					
				case 'getservers':
					$this->vatsimservers();
					break;
					
				case 'getip':
					$this->getip();
				break;
			}
			
		}
	}
	
	public function ConvertMinutes2Hours($Minutes)
	{
		if ($Minutes < 0)
		{
			$Min = Abs($Minutes);
		}
		else
		{
			$Min = $Minutes;
		}
		$iHours = Floor($Min / 60);
		$Minutes = ($Min - ($iHours * 60)) / 100;
		$tHours = $iHours + $Minutes;
		if ($Minutes < 0)
		{
			$tHours = $tHours * (-1);
		}
		$aHours = explode(".", $tHours);
		$iHours = $aHours[0];
		if (empty($aHours[1]))
		{
			$aHours[1] = "00";
		}
		$Minutes = $aHours[1];
		if (strlen($Minutes) < 2)
		{
			$Minutes = $Minutes ."0";
		}
		$tHours = $iHours .":". $Minutes;
		return $tHours;
	}
	
	/*public function getLatestBid2($pilotid)
	{
		$pilotid = DB::escape($pilotid);
		
		$sql = 'SELECT s.*, b.bidid, a.id as aircraftid, a.name as aircraft, a.registration, a.maxpax, a.maxcargo
				FROM '.TABLE_PREFIX.'schedules s, 
					 '.TABLE_PREFIX.'bids b,
					 '.TABLE_PREFIX.'aircraft a
				WHERE b.routeid = s.id 
					AND s.aircraft=a.id
					AND b.pilotid='.$pilotid.'
				ORDER BY b.bidid ASC LIMIT 1';
		
		return DB::get_row($sql);
	}*/
	
	public function sendXML($params)
	{
		$xml = new SimpleXMLElement("<sitedata />");
		
		$info_xml = $xml->addChild('info');
		foreach($params as $name => $value)
		{
			$info_xml->addChild($name, $value);
		}
		
		header('Content-type: text/xml'); 		
		$xml_string = $xml->asXML();
		echo $xml_string;
		
		# For debug
		#$this->log("Sending: \n".print_r($xml_string, true), 'kacars');
		
		return;	
	}
	
	public static function getAllAircraft()
	{
		$results = OperationsData::getAllAircraft(true);
		
		$xml = new SimpleXMLElement("<aircraftdata />");
		
		$info_xml = $xml->addChild('info');
		
		foreach($results as $row)
		{
			$info_xml->addChild('aircraftICAO', $row->icao);
			$info_xml->addChild('aircraftReg', $row->registration);
		}
		
		# For debug
		#$this->log("Sending: \n".print_r($xml_string, true), 'kacars');
		
		header('Content-type: text/xml');
		echo $xml->asXML();
	}
	
	public static function ProcessLogin($useridoremail, $password)
	{
		# Allow them to login in any manner:
		#  Email: blah@blah.com
		#  Pilot ID: VMA0001, VMA 001, etc
		#  Just ID: 001
		if(is_numeric($useridoremail))
		{
			$useridoremail =  $useridoremail - intval(Config::Get('PILOTID_OFFSET'));
			$sql = 'SELECT * FROM '.TABLE_PREFIX.'pilots
				   WHERE pilotid='.$useridoremail;
		}
		else
		{
			if(preg_match('/^.*\@.*$/i', $useridoremail) > 0)
			{
				$emailaddress = DB::escape($useridoremail);
				$sql = 'SELECT * FROM ' . TABLE_PREFIX . 'pilots
					   WHERE email=\''.$useridoremail.'\'';
			} 
			
			elseif(preg_match('/^([A-Za-z]*)(.*)(\d*)/', $useridoremail, $matches)>0)
			{
				$id = trim($matches[2]);
				$id = $id - intval(Config::Get('PILOTID_OFFSET'));
				
				$sql = 'SELECT * FROM '.TABLE_PREFIX.'pilots
					   WHERE pilotid='.$id;
			}
			else
			{				
				return false;
			}
		}
		
		$password = DB::escape($password);
		$userinfo = DB::get_row($sql);

		if(!$userinfo)
		{			
			return false;
		}
		
		if($userinfo->retired == 1)
		{			
			return false;
		}

		//ok now check it
		$hash = md5($password . $userinfo->salt);
		
		if($hash == $userinfo->password)
		{						
			return true;
		}			
		else 
		{					
			return false;
		}
	}
	
		////////////////// SAVCARS /////////////////
		////////////////////////////////////////////
		////////////////////////////////////////////
	public function showSchedules()
	{
		$xml = new SimpleXMLElement("<schedules />");
		
	
		
		
		$info_xml = $xml->addChild('schedule');

		$info_xml->addChild('count', '0');
			$info_xml->addChild('flightnum', '0');
			$info_xml->addChild('depicao', '0');
			$info_xml->addChild('arricao','0');
			$info_xml->addChild('type', '0');
			$info_xml->addChild('deptime', '0');
			$info_xml->addChild('arrtime', '0');
			$info_xml->addChild('aircraft', '0');
			$info_xml->addChild('registration', '0');
			$info_xml->addChild('distance', '0');
			$info_xml->addChild('route', '0');
			$info_xml->addChild('dow', '0');
		
		header('Content-type: text/xml');
		echo $xml->asXML();

	}
		
		
		
		public function versioncheck($currentversion)
	{												//http://www.alphastone.co.uk/severnair
				$latestversion = file_get_contents('http://www.alphastone.co.uk/severnair/phpvms/core/modules/SAVCARS/latestversion.txt');
				
				if ($latestversion > $currentversion) {

					if ($handle = opendir('core/modules/SAVCARS')) {
						while (false !== ($entry = readdir($handle))) {
							if ($entry != "." && $entry != "..") {
								if (preg_match('/exe/',$entry)) {
								
								$newurl = "http://www.alphastone.co.uk/severnair/phpvms/core/modules/SAVCARS/" . $entry;
								}
								
							}
						}
						closedir($handle);
					}
						
						
						
						
						$latestversion = str_replace(".","_",$latestversion);
					$params = array(
					'newversionavailable' 	   => '1',
					'versionavailable' 	   => $latestversion,
					'url' 	   => $newurl	
					);	
				

	
		
						
				
				}
				else
				{
					$params = array(
					'newversionavailable' 	   => '0'	
					);	
				}
				
						
						
					
				$send = $this->sendXML($params);


	}
	
	
	public $section=null;
    public $count = 0;

    public function get_vatsim_data($find) {
	
    //get new file from Vatsim if the existing one is more than 5 minutes old
        if(!file_exists('vatsimdata.txt') || time()-filemtime('vatsimdata.txt') > 120) {
		//echo 2;
        //choose a random download location per Vatsim policy
            $random = (rand(1, 5));
            if ($random == 1) {$url = 'http://www.pcflyer.net/DataFeed/vatsim-data.txt';}
            if ($random == 2) {$url = 'http://www.klain.net/sidata/vatsim-data.txt';}
            if ($random == 3) {$url = 'http://fsproshop.com/servinfo/vatsim-data.txt';}
            if ($random == 4) {$url = 'http://info.vroute.net/vatsim-data.txt';}
			if ($random == 5) {$url = 'http://data.vattastic.com/vatsim-data.txt';}
            $newfile="vatsimdata.txt";
			copy($url,$newfile);
        }

        $contents = file('vatsimdata.txt');
        $this->section = array();
        $readsection = false;
        foreach($contents as $line) {
            if(preg_match("/.*{$find}.*/i", $line, $matches)) {
                $readsection = true;
                continue;
            }
            if($readsection) {
                if(trim($line) == ';') {
                    break;
                }

                $this->section[] = $line;
				
                $count=$this->count++;
				//var_dump($this->section);
            }
        }
    }

    public function create_vatsim_data($find, $type, $ID) {
        if($this->section == null)
            $this->get_vatsim_data($find);
                
		#start xml
		$xml = new SimpleXMLElement("<vatsim />");
		$info_xml = $xml->addChild('info');
		$i = 0;
        foreach ($this->section as $row) {
            $row_info = explode(":", $row);
            if (preg_match("/^$type/", $row_info[3])) {
                if ($ID == $row_info[1]) {
				$info_xml->addChild('available', '1');
				$info_xml->addChild('callsign', $row_info[0]);
				$info_xml->addChild('depicao', $row_info[11]);
				$info_xml->addChild('arricao', $row_info[13]);
				$info_xml->addChild('altitude', $row_info[12]);
				$info_xml->addChild('route', $row_info[30]);
				$info_xml->addChild('ac', $row_info[9]);

					$i++;
                }
            }
		
		
		}
		if ($i == 0)
		{
			$info_xml->addChild('available', '0');
		}
		
		header('Content-type: text/xml');
		echo $info_xml->asXML();
		#End exml
    }
	
	
	
	public function create_vatsim_data1($find, $type, $callsign) {

		$xml = new SimpleXMLElement("<vatsim />");
		$info_xml = $xml->addChild('info');
			$info_xml->addChild('available', '0');	
		header('Content-type: text/xml');
		echo $info_xml->asXML();
		#End exml
    }
	
	public static function SetAll() {
	if ($_GET["set"] == "true" or $_GET["set"] == "false") {
	
                $sql = "SELECT pilotid FROM ".TABLE_PREFIX."pilots";
	
					 $ret = DB::get_results($sql);
               //var_dump($ret);
			
		//$me = PilotData::findPilots('');
		$me_count = count($ret);
		$z = 0;
		foreach ($ret as $pilot) {
			if ($z >= $me_count) {
			break;
		}
		echo PilotData::saveFields($pilot->pilotid, array('SCHEDULE_DOWNLOAD'=>$_GET["set"]));
		echo " - " . $pilot->pilotid;
		echo "<br />";
		$z++;
		}
		//var_dump($me);			
					
		}			
	}
	
	
	    public function vatsimonlinecheck($find, $type, $ID) {
        if($this->section == null)
            $this->get_vatsim_data($find, $type, $ID);
                
		#start xml
		$xml = new SimpleXMLElement("<vatsim />");
		$info_xml = $xml->addChild('info');
		$i = 0;
        foreach ($this->section as $row) {
            $row_info = explode(":", $row);
            if (preg_match("/^$type/", $row_info[3])) {
                if ($ID == $row_info[1]) {
				$info_xml->addChild('online', '1');
				
                   # Template::Set('row_info', $row_info);
                    #Template::Show('vatsim/vatsim.tpl');
					$i++;
                }
            }
		
		
		}
		if ($i == 0)
		{
			$info_xml->addChild('online', '0');
		}
		
		header('Content-type: text/xml');
		echo $info_xml->asXML();
		#End exml
    }
	
	
	public function vatsimservers() {
        if($this->section == null) 
            $this->get_vatsim_data('!SERVERS:');
			
        $i = 0;
		#start xml

		$xml = new SimpleXMLElement("<servers />");
		
		$info_xml = $xml->addChild('info');
		
		foreach($this->section as $row)
		{
            $row_info = explode(":", $row);
			$info_xml->addChild('url', $row_info[1]);

		$i++;
		}
		$info_xml->addAttribute('count', $i);
		# For debug
		#$this->log("Sending: \n".print_r($xml_string, true), 'kacars');
		
		header('Content-type: text/xml');
		echo $xml->asXML();
		
    }
	
		public function getip() {
			
 
		#start xml
		$allpilots = PilotData::findPilots(0);
		if(!$allpilots)
		{
			$allpilots = array();
		}
               //var_dump($ret);
			
		//$me = PilotData::findPilots('');
		$me_count = count($allpilots);
		$z = 0;
		$xml = new SimpleXMLElement("<servers />");
		$info_xml = $xml->addChild('info');
		
		foreach ($allpilots as $pilot) {
			if ($z >= $me_count) {
			break;
		}
			$info_xml->addChild('ip', $pilot->lastip);
		$z++;
		}
		
		# For debug
		#$this->log("Sending: \n".print_r($xml_string, true), 'kacars');
		
		header('Content-type: text/xml');
		echo $xml->asXML();
		
    }
	
}
